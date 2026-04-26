namespace RetailDemandForecastingAPI.DTO
{
    public class SalesInput
    {
        public string StoreCode { get; set; }
        public string ProductCode { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}
