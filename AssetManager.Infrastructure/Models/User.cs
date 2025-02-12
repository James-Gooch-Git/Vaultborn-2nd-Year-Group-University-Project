namespace AssetManager.Infrastructure.Models
{
    public class User
    {
        public int Id { get; set; } //change to string
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } //get rid - don't need
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}