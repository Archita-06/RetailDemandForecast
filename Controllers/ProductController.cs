using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(AppDbContext context) : ControllerBase

    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public IActionResult GetAll()
        {
            var products= _context.Products.ToList();
            return Ok(products);
        }

        [HttpPost]
        public IActionResult Create(Product product) {
            _context.Products.Add(product);
            _context.SaveChanges();
            return Ok(product);
        }
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateTask(List<Product> products)
        {
            if (products == null || products.Count == 0)
            {
                return BadRequest("No products provided");
            }
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            return Ok(new { inserted = products.Count });
        }
    }

    
}
