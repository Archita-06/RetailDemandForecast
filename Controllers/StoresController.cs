using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stores = await _context.Stores.ToListAsync();
            return Ok(stores);
        }


        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Store code is required");

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreCode == code);

            if (store == null)
                return NotFound($"Store with code {code} not found");

            return Ok(store);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Store store)
        {
            if (store == null)
                return BadRequest("Invalid store");

            if (string.IsNullOrWhiteSpace(store.StoreCode))
                return BadRequest("Store code is required");

            store.StoreCode = store.StoreCode.Trim();

            var exists = await _context.Stores
                .AnyAsync(s => s.StoreCode == store.StoreCode);

            if (exists)
                return BadRequest($"Store with code {store.StoreCode} already exists");

            await _context.Stores.AddAsync(store);
            await _context.SaveChangesAsync();

            return Ok(store);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk(List<Store> stores)
        {
            if (stores == null || stores.Count == 0)
                return BadRequest("No stores provided");

            var skippedItems = new List<object>();

            
            var validStores = new List<Store>();

            foreach (var s in stores)
            {
                if (string.IsNullOrWhiteSpace(s.StoreCode))
                {
                    skippedItems.Add(new
                    {
                        code = (string?)null,
                        reason = "Missing StoreCode"
                    });
                    continue;
                }

                s.StoreCode = s.StoreCode.Trim().ToUpper();
                validStores.Add(s);
            }

          
            var distinctStores = new List<Store>();
            var seen = new HashSet<string>();

            foreach (var s in validStores)
            {
                if (!seen.Add(s.StoreCode))
                {
                    skippedItems.Add(new
                    {
                        code = s.StoreCode,
                        reason = "Duplicate in request"
                    });
                }
                else
                {
                    distinctStores.Add(s);
                }
            }

           
            var existingCodes = await _context.Stores
                .Select(s => s.StoreCode)
                .ToListAsync();

            var existingSet = existingCodes.ToHashSet();

            
            var newStores = new List<Store>();

            foreach (var s in distinctStores)
            {
                if (existingSet.Contains(s.StoreCode))
                {
                    skippedItems.Add(new
                    {
                        code = s.StoreCode,
                        reason = "Already exists in database"
                    });
                }
                else
                {
                    newStores.Add(s);
                }
            }

           
            if (newStores.Count > 0)
            {
                await _context.Stores.AddRangeAsync(newStores);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                inserted = newStores.Count,
                skipped = skippedItems.Count,
                skippedItems
            });
        }


        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreCode == code);

            if (store == null)
                return NotFound($"Store with code {code} not found");

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            return Ok($"Store {code} deleted");
        }
    }
}