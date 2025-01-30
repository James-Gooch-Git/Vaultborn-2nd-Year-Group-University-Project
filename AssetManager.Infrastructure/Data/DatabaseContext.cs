using AssetManager.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssetManager.Infrastructure.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<ModelVersion> ModelVersions { get; set; }
        public DbSet<UploadQueue> UploadQueue { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=AssetManagerDB;Username=postgres;Password=");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Model>().ToTable("models");
            modelBuilder.Entity<ModelVersion>().ToTable("model_versions");
            modelBuilder.Entity<UploadQueue>().ToTable("upload_queue");
        }
    }
}