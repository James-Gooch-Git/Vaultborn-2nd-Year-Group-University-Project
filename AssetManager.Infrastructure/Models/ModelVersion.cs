namespace AssetManager.Infrastructure.Models
{
    public class ModelVersion
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public int UserId { get; set; }
        public int VersionNumber { get; set; }
        public string FileUrn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}