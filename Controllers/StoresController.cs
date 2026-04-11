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
        [HttpGet]
        public IActionResult GetAll() {
            var stores=_context.Stores.ToList();

            return Ok(stores);
        }
    }
}
