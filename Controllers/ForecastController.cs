using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using RetailDemandForecastingAPI.Domain.Entities;
using RetailDemandForecastingAPI.Persistence;

namespace RetailDemandForecastingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForecastController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ForecastController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{storeCode}/{productCode}")]
        public async Task<IActionResult> Forecast(string storeCode, string productCode, int days = 7)
        {
            
            if (days <= 0 || days > 90)
                return BadRequest("Days must be between 1 and 90");

           
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
                .Select(s => (float)s.QuantitySold)
                .ToListAsync();

            if (sales.Count < 8)
                return BadRequest("At least 8 days of data required");

         
            var trainingData = new List<SalesData>();

            for (int i = 0; i < sales.Count - 7; i++)
            {
                trainingData.Add(new SalesData
                {
                    Day1 = sales[i],
                    Day2 = sales[i + 1],
                    Day3 = sales[i + 2],
                    Day4 = sales[i + 3],
                    Day5 = sales[i + 4],
                    Day6 = sales[i + 5],
                    Day7 = sales[i + 6],
                    Label = sales[i + 7]
                });
            }

        
            var mlContext = new MLContext();

            var data = mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = mlContext.Transforms.Concatenate("Features",
                    nameof(SalesData.Day1),
                    nameof(SalesData.Day2),
                    nameof(SalesData.Day3),
                    nameof(SalesData.Day4),
                    nameof(SalesData.Day5),
                    nameof(SalesData.Day6),
                    nameof(SalesData.Day7))
                .Append(mlContext.Regression.Trainers.FastTree());

            var model = pipeline.Fit(data);

            var engine = mlContext.Model.CreatePredictionEngine<SalesData, ForecastPrediction>(model);

      
            var last7 = sales.Skip(sales.Count - 7).ToList();
            var predictions = new List<float>();

            for (int i = 0; i < days; i++)
            {
                var input = new SalesData
                {
                    Day1 = last7[0],
                    Day2 = last7[1],
                    Day3 = last7[2],
                    Day4 = last7[3],
                    Day5 = last7[4],
                    Day6 = last7[5],
                    Day7 = last7[6]
                };

                var prediction = engine.Predict(input).Score;

                prediction = (float)Math.Round(prediction, 2);

                predictions.Add(prediction);

             
                last7.RemoveAt(0);
                last7.Add(prediction);
            }

            var lastDate = await _context.Sales
                .Where(s => s.StoreId == storeId && s.ProductId == productId)
                .MaxAsync(s => s.Date);

          
            var oldForecasts = _context.Forecasts
                .Where(f => f.StoreId == storeId && f.ProductId == productId);

            _context.Forecasts.RemoveRange(oldForecasts);


            var forecastEntities = new List<Forecast>();

            for (int i = 0; i < predictions.Count; i++)
            {
                forecastEntities.Add(new Forecast
                {
                    StoreId = storeId,
                    ProductId = productId,
                    ForecastDate = lastDate.AddDays(i + 1),
                    PredictedDemand = predictions[i]
                });
            }

            await _context.Forecasts.AddRangeAsync(forecastEntities);
            await _context.SaveChangesAsync();

        
            var response = forecastEntities.Select(f => new
            {
                date = f.ForecastDate,
                predictedDemand = f.PredictedDemand
            });

            return Ok(new
            {
                storeCode,
                productCode,
                days,
                forecast = response
            });
        }

        [HttpGet("stored/{storeCode}/{productCode}")]
        public async Task<IActionResult> GetStoredForecast(string storeCode, string productCode)
        {
            var storeId = await _context.Stores
                .Where(s => s.StoreCode == storeCode)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            var productId = await _context.Products
                .Where(p => p.ProductCode == productCode)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var forecasts = await _context.Forecasts
                .Where(f => f.StoreId == storeId && f.ProductId == productId)
                .OrderBy(f => f.ForecastDate)
                .ToListAsync();

            return Ok(forecasts);
        }
    }

    public class SalesData
    {
        public float Day1;
        public float Day2;
        public float Day3;
        public float Day4;
        public float Day5;
        public float Day6;
        public float Day7;

        public float Label;
    }

    public class ForecastPrediction
    {
        public float Score;
    }
}