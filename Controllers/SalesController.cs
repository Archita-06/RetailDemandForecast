using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.DTO;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sales = await _context.Sales
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold,
                    StoreCode = s.Store.StoreCode,
                    ProductCode = s.Product.ProductCode
                })
                .ToListAsync();

            return Ok(sales);
        }

        
        [HttpGet("{storeCode}/{productCode}")]
        public async Task<IActionResult> GetProductSalesInStore(string storeCode, string productCode)
        {
            var storeId = await _context.Stores
                .Where(s => s.StoreCode == storeCode)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            var productId = await _context.Products
                .Where(p => p.ProductCode == productCode)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0 || productId == 0)
                return NotFound("Invalid store or product");

            var sales = await _context.Sales
                .Where(s => s.StoreId == storeId && s.ProductId == productId)
                .OrderBy(s => s.Date)
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold,
                    s.BatchId,
                    s.UploadType
                })
                .ToListAsync();

            return Ok(sales);
        }

       
        [HttpGet("store/{storeCode}")]
        public async Task<IActionResult> GetAllSalesForStore(string storeCode)
        {
            var storeId = await _context.Stores
                .Where(s => s.StoreCode == storeCode)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0)
                return NotFound("Invalid store");

            var sales = await _context.Sales
                .Where(s => s.StoreId == storeId)
                .OrderBy(s => s.Date)
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold,
                    ProductCode = s.Product.ProductCode
                })
                .ToListAsync();

            return Ok(sales);
        }

        
        [HttpGet("product/{productCode}")]
        public async Task<IActionResult> GetAllSalesForProduct(string productCode)
        {
            var productId = await _context.Products
                .Where(p => p.ProductCode == productCode)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (productId == 0)
                return NotFound("Invalid product");

            var sales = await _context.Sales
                .Where(s => s.ProductId == productId)
                .OrderBy(s => s.Date)
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold,
                    StoreCode = s.Store.StoreCode
                })
                .ToListAsync();

            return Ok(sales);
        }

   
        [HttpGet("range")]
        public async Task<IActionResult> GetSalesByDateRange(DateTime start, DateTime end)
        {
            var sales = await _context.Sales
                .Where(s => s.Date >= start && s.Date <= end)
                .OrderBy(s => s.Date)
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold,
                    StoreCode = s.Store.StoreCode,
                    ProductCode = s.Product.ProductCode
                })
                .ToListAsync();

            return Ok(sales);
        }

        [HttpGet("daily/{storeCode}/{productCode}")]
        public async Task<IActionResult> GetDailySales(string storeCode, string productCode)
        {
            var storeId = await _context.Stores
                .Where(s => s.StoreCode == storeCode)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            var productId = await _context.Products
                .Where(p => p.ProductCode == productCode)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0 || productId == 0)
                return NotFound("Invalid store or product");

            var sales = await _context.Sales
                .Where(s => s.StoreId == storeId && s.ProductId == productId)
                .OrderBy(s => s.Date)
                .Select(s => new
                {
                    s.Date,
                    s.QuantitySold
                })
                .ToListAsync();

            return Ok(sales);
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadSales(IFormFile file, [FromQuery] UploadType uploadType)
        {
            var upload = new DataUpload
            {
                FileName = file.FileName,
                UploadedAt = DateTime.UtcNow,
                Status = "PROCESSING"
            };

            await _context.DataUploads.AddAsync(upload);
            await _context.SaveChangesAsync();

            try
            {
                if (file == null || file.Length == 0)
                {
                    upload.Status = "FAILED";
                    await _context.SaveChangesAsync();
                    return BadRequest("File is empty");
                }

                var extension = Path.GetExtension(file.FileName).ToLower();

                List<SalesInput> rawData;
                int skipped;

                if (extension == ".csv")
                    (rawData, skipped) = await ProcessCsv(file);
                else if (extension == ".xlsx")
                    (rawData, skipped) = await ProcessExcel(file);
                else
                {
                    upload.Status = "FAILED";
                    await _context.SaveChangesAsync();
                    return BadRequest("Unsupported file format.");
                }

                if (rawData.Count == 0)
                {
                    upload.Status = "FAILED";
                    await _context.SaveChangesAsync();
                    return BadRequest("No valid data");
                }

                var groupedData = rawData
                    .GroupBy(x => new { x.StoreCode, x.ProductCode, x.Date })
                    .Select(g => new
                    {
                        g.Key.StoreCode,
                        g.Key.ProductCode,
                        g.Key.Date,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToList();

                var batchId = Guid.NewGuid().ToString();

                int inserted = 0, updated = 0;

                foreach (var item in groupedData)
                {
                    var store = await _context.Stores
                        .FirstOrDefaultAsync(s => s.StoreCode == item.StoreCode);

                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductCode == item.ProductCode);

                    if (store == null || product == null)
                    {
                        skipped++;
                        continue;
                    }

                    var existing = await _context.Sales.FirstOrDefaultAsync(s =>
                        s.StoreId == store.Id &&
                        s.ProductId == product.Id &&
                        s.Date == item.Date);

                    if (existing == null)
                    {
                        await _context.Sales.AddAsync(new Sale
                        {
                            StoreId = store.Id,
                            ProductId = product.Id,
                            Date = item.Date,
                            QuantitySold = item.Quantity,
                            BatchId = batchId,
                            UploadType = uploadType
                        });

                        inserted++;
                    }
                    else
                    {
                        if (uploadType == UploadType.INCREMENTAL)
                        {
                            existing.QuantitySold += item.Quantity;
                        }
                        else if (uploadType == UploadType.SNAPSHOT &&
                                 item.Quantity > existing.QuantitySold)
                        {
                            existing.QuantitySold = item.Quantity;
                        }

                        existing.BatchId = batchId;
                        existing.UploadType = uploadType;

                        updated++;
                    }
                }

                await _context.SaveChangesAsync();

                upload.Status = "SUCCESS";
                await _context.SaveChangesAsync();

                return Ok(new { batchId, inserted, updated, skipped });
            }
            catch (Exception)
            {
                upload.Status = "FAILED";
                await _context.SaveChangesAsync();
                throw;
            }
        }


        private async Task<(List<SalesInput>, int)> ProcessCsv(IFormFile file)
        {
            var list = new List<SalesInput>();
            int skipped = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            await reader.ReadLineAsync();

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = line.Split(',');

                if (values.Length < 4)
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(values[2], out var date) ||
                    !int.TryParse(values[3], out var qty))
                {
                    skipped++;
                    continue;
                }

                list.Add(new SalesInput
                {
                    StoreCode = values[0].Trim(),
                    ProductCode = values[1].Trim(),
                    Date = date,
                    Quantity = qty
                });
            }

            return (list, skipped);
        }

        private async Task<(List<SalesInput>, int)> ProcessExcel(IFormFile file)
        {
            var list = new List<SalesInput>();
            int skipped = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            ExcelPackage.License.SetNonCommercialPersonal("Dev");

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            int rows = worksheet.Dimension.Rows;

            for (int row = 2; row <= rows; row++)
            {
                var storeCode = worksheet.Cells[row, 1].Text.Trim();
                var productCode = worksheet.Cells[row, 2].Text.Trim();

                if (!DateTime.TryParse(worksheet.Cells[row, 3].Text, out var date) ||
                    !int.TryParse(worksheet.Cells[row, 4].Text, out var qty))
                {
                    skipped++;
                    continue;
                }

                list.Add(new SalesInput
                {
                    StoreCode = storeCode,
                    ProductCode = productCode,
                    Date = date,
                    Quantity = qty
                });
            }

            return (list, skipped);
        }
    }
}