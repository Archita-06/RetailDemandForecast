namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class Forecast
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public DateTime ForecastDate { get; set; }
        public double PredictedDemand { get; set; }
    }
}
