namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class Store
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
    }
}
