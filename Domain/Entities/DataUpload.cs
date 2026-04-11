namespace RetailDemandForecastingAPI.Domain.Entities
{
    public class DataUpload
    {

        public int Id { get; set; }
        public required string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public required string Status { get; set; }


    }
}
