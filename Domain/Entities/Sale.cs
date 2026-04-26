namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; } = null;

        public int StoreId { get; set; }
        public Store? Store { get; set; } = null!;

        public DateTime Date { get; set; }
        public int QuantitySold { get; set; }
        public string BatchId { get; set; } = null!;

        public UploadType UploadType { get; set; }
    }
}
