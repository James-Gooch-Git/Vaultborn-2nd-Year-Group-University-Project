namespace AssetManager.Infrastructure.Models
{
    public class UploadQueue
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public string FilePath { get; set; }
        public string Status { get; set; } = "pending"; // pending -> uploaded / failed
    }
}