using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Inventories.ToListAsync();
            return Ok(data);
        }

 
        [HttpGet("{storeId}/{productId}")]
        public async Task<IActionResult> Get(int storeId, int productId)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == productId);

            if (inv == null)
                return NotFound("Inventory not found");

            return Ok(inv);
        }

     
        [HttpPost]
        public async Task<IActionResult> Create(Inventory inventory)
        {
            await _context.Inventories.AddAsync(inventory);
            await _context.SaveChangesAsync();

            return Ok(inventory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Inventory updated)
        {
            var inv = await _context.Inventories.FindAsync(id);

            if (inv == null)
                return NotFound("Inventory not found");

            inv.stockLevel = updated.stockLevel;
            inv.ProductId = updated.ProductId;
            inv.StoreId = updated.StoreId;

            await _context.SaveChangesAsync();

            return Ok(inv);
        }

    
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var inv = await _context.Inventories.FindAsync(id);

            if (inv == null)
                return NotFound("Inventory not found");

            _context.Inventories.Remove(inv);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }
    }
}