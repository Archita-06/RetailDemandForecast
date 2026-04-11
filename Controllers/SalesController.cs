using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public IActionResult GetAll()
        {
            var sales = _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Store)
                .ToList();

            return Ok(sales);
        }

        [HttpPost]
        public IActionResult Create(Sale sale) {
            _context.Sales.Add(sale);
            _context.SaveChanges();
            return Ok(sale);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSales(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var salesList = new List<Sale>();
            int skippedRows = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                await reader.ReadLineAsync(); // skip header

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = line.Split(',');

                    if (values.Length < 4)
                    {
                        skippedRows++;
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
                        skippedRows++;
                        continue;
                    }

                    if (!DateTime.TryParse(values[2], out var date))
                    {
                        skippedRows++;
                        continue;
                    }

                    if (!int.TryParse(values[3], out var quantity))
                    {
                        skippedRows++;
                        continue;
                    }

                    var sale = new Sale
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        Date = date,
                        QuantitySold = quantity
                    };

                    salesList.Add(sale);
                }
            }

            if (salesList.Count == 0)
                return BadRequest("No valid data found in file");

            await _context.Sales.AddRangeAsync(salesList);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                inserted = salesList.Count,
                skipped = skippedRows
            });
        }

        [HttpPost("upload-single-store")]
        public async Task<IActionResult> UploadSalesForStore(
    IFormFile file,
    [FromQuery] int storeId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var store = _context.Stores.Find(storeId);
            if (store == null)
                return BadRequest("Invalid Store");

            var salesList = new List<Sale>();
            int skipped = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                await reader.ReadLineAsync(); // skip header

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
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
                        StoreId = storeId, // 🔥 comes from dropdown
                        Date = date,
                        QuantitySold = qty
                    });
                }
            }

            await _context.Sales.AddRangeAsync(salesList);
            await _context.SaveChangesAsync();

            return Ok(new { inserted = salesList.Count, skipped });
        }

    }
}
