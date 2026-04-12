using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;
using OfficeOpenXml;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public IActionResult GetAll()
        {
            var sales = _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Store)
                .ToList();

            return Ok(sales);
        }

        
        [HttpPost("upload")]
        public async Task<IActionResult> UploadSales(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            var extension = Path.GetExtension(file.FileName).ToLower();

            List<Sale> salesList;
            int skipped;

            if (extension == ".csv")
            {
                (salesList, skipped) = await ProcessCsv(file);
            }
            else if (extension == ".xlsx")
            {
                (salesList, skipped) = await ProcessExcel(file);
            }
            else
            {
                return BadRequest("Unsupported file type");
            }

            if (salesList.Count == 0)
                return BadRequest("No valid data found");

            await _context.Sales.AddRangeAsync(salesList);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                inserted = salesList.Count,
                skipped
            });
        }

  
        [HttpPost("upload-single-store")]
        public async Task<IActionResult> UploadSalesForStore(IFormFile file, [FromQuery] int storeId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var store = _context.Stores.Find(storeId);
            if (store == null)
                return BadRequest("Invalid store");

            var extension = Path.GetExtension(file.FileName).ToLower();

            List<Sale> salesList;
            int skipped;

            if (extension == ".csv")
            {
                (salesList, skipped) = await ProcessCsvSingleStore(file, storeId);
            }
            else if (extension == ".xlsx")
            {
                (salesList, skipped) = await ProcessExcelSingleStore(file, storeId);
            }
            else
            {
                return BadRequest("Unsupported file type");
            }

            if (salesList.Count == 0)
                return BadRequest("No valid data found");

            await _context.Sales.AddRangeAsync(salesList);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                inserted = salesList.Count,
                skipped
            });
        }

        

        private async Task<(List<Sale>, int)> ProcessCsv(IFormFile file)
        {
            var salesList = new List<Sale>();
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

                var productName = values[0].Trim();
                var storeName = values[1].Trim();

                var product = _context.Products
                    .FirstOrDefault(p => p.Name.ToLower() == productName.ToLower());

                var store = _context.Stores
                    .FirstOrDefault(s => s.Name.ToLower() == storeName.ToLower());

                if (product == null || store == null)
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(values[2], out var date) ||
                    !int.TryParse(values[3], out var quantity))
                {
                    skipped++;
                    continue;
                }

                salesList.Add(new Sale
                {
                    ProductId = product.Id,
                    StoreId = store.Id,
                    Date = date,
                    QuantitySold = quantity
                });
            }

            return (salesList, skipped);
        }

       

        private async Task<(List<Sale>, int)> ProcessExcel(IFormFile file)
        {
            var salesList = new List<Sale>();
            int skipped = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            ExcelPackage.License.SetNonCommercialPersonal("Dev");

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var productName = worksheet.Cells[row, 1].Text.Trim();
                var storeName = worksheet.Cells[row, 2].Text.Trim();
                var dateText = worksheet.Cells[row, 3].Text;
                var qtyText = worksheet.Cells[row, 4].Text;

                var product = _context.Products
                    .FirstOrDefault(p => p.Name.ToLower() == productName.ToLower());

                var store = _context.Stores
                    .FirstOrDefault(s => s.Name.ToLower() == storeName.ToLower());

                if (product == null || store == null)
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(dateText, out var date) ||
                    !int.TryParse(qtyText, out var qty))
                {
                    skipped++;
                    continue;
                }

                salesList.Add(new Sale
                {
                    ProductId = product.Id,
                    StoreId = store.Id,
                    Date = date,
                    QuantitySold = qty
                });
            }

            return (salesList, skipped);
        }

      

        private async Task<(List<Sale>, int)> ProcessCsvSingleStore(IFormFile file, int storeId)
        {
            var salesList = new List<Sale>();
            int skipped = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            await reader.ReadLineAsync();

            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = line.Split(',');

                if (values.Length < 3)
                {
                    skipped++;
                    continue;
                }

                var productName = values[0].Trim();

                var product = _context.Products
                    .FirstOrDefault(p => p.Name.ToLower() == productName.ToLower());

                if (product == null)
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(values[1], out var date) ||
                    !int.TryParse(values[2], out var qty))
                {
                    skipped++;
                    continue;
                }

                salesList.Add(new Sale
                {
                    ProductId = product.Id,
                    StoreId = storeId,
                    Date = date,
                    QuantitySold = qty
                });
            }

            return (salesList, skipped);
        }

      

        private async Task<(List<Sale>, int)> ProcessExcelSingleStore(IFormFile file, int storeId)
        {
            var salesList = new List<Sale>();
            int skipped = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            ExcelPackage.License.SetNonCommercialPersonal("Dev");

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var productName = worksheet.Cells[row, 1].Text.Trim();
                var dateText = worksheet.Cells[row, 2].Text;
                var qtyText = worksheet.Cells[row, 3].Text;

                var product = _context.Products
                    .FirstOrDefault(p => p.Name.ToLower() == productName.ToLower());

                if (product == null)
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(dateText, out var date) ||
                    !int.TryParse(qtyText, out var qty))
                {
                    skipped++;
                    continue;
                }

                salesList.Add(new Sale
                {
                    ProductId = product.Id,
                    StoreId = storeId,
                    Date = date,
                    QuantitySold = qty
                });
            }

            return (salesList, skipped);
        }
    }
}