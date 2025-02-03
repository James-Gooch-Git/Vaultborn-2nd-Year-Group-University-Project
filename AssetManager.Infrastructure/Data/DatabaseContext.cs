using AssetManager.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AssetManager.Infrastructure.Data
{
    public class DatabaseContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<User> Users { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<ModelVersion> ModelVersions { get; set; }
        public DbSet<UploadQueue> UploadQueue { get; set; }

        public DatabaseContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Model>().ToTable("models");
            modelBuilder.Entity<ModelVersion>().ToTable("model_versions");
            modelBuilder.Entity<UploadQueue>().ToTable("upload_queue");
            
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "12345", Email = "tom.green@example.com" },
                new User { Id = 2, Username = "67890", Email = "alice.brown@example.com" }
            );
        }
    }
}