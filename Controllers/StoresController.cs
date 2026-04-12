using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoresController(AppDbContext context) {

            _context = context;
        }
        [HttpPost]
        public IActionResult Create(Store store) {

            _context.Stores.Add(store);
            _context.SaveChanges();
            return Ok(store);

        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateTask(List<Store> stores) {
            if (stores==null|| stores.Count==0) {
                return BadRequest("No stores provided");
            }
            await _context.Stores.AddRangeAsync(stores);
            await _context.SaveChangesAsync();

            return Ok(new { inserted = stores.Count });
        }


        [HttpGet]
        public IActionResult GetAll() {
            var stores=_context.Stores.ToList();

            return Ok(stores);
        }

    }
}
