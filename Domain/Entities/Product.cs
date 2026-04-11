namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Category { get; set; }

    }
}
