using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Product code is required");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductCode == code);

            if (product == null)
                return NotFound($"Product with code {code} not found");

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (product == null)
                return BadRequest("Invalid product");

            if (string.IsNullOrWhiteSpace(product.ProductCode))
                return BadRequest("Product code is required");

            // Trim to avoid hidden duplicates
            product.ProductCode = product.ProductCode.Trim();

            var exists = await _context.Products
                .AnyAsync(p => p.ProductCode == product.ProductCode);

            if (exists)
                return BadRequest($"Product with code {product.ProductCode} already exists");

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }


        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk(List<Product> products)
        {
            if (products == null || products.Count == 0)
                return BadRequest("No products provided");

            
            var validProducts = products
                .Where(p => !string.IsNullOrWhiteSpace(p.ProductCode))
                .Select(p =>
                {
                    p.ProductCode = p.ProductCode.Trim();
                    return p;
                })
                .ToList();

            var distinctProducts = validProducts
                .GroupBy(p => p.ProductCode)
                .Select(g => g.First())
                .ToList();

            var existingCodes = await _context.Products
                .Select(p => p.ProductCode)
                .ToListAsync();

            var existingSet = existingCodes.ToHashSet();

          
            var newProducts = distinctProducts
                .Where(p => !existingSet.Contains(p.ProductCode))
                .ToList();

            await _context.Products.AddRangeAsync(newProducts);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                inserted = newProducts.Count,
                skipped = products.Count - newProducts.Count
            });
        }


        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductCode == code);

            if (product == null)
                return NotFound($"Product with code {code} not found");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok($"Product {code} deleted");
        }
    }
}