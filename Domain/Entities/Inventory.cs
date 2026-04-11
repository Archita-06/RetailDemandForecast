namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class Inventory
    {
        public int ID { get; set; }
        public int ProductId { get; set; }
        public int StoreId { get; set; }

        public int stockLevel { get; set; }


    }
}
