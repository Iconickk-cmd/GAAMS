using GAAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.Infrastructure.Data;

public class GAAMSDbContext : DbContext
{
    public GAAMSDbContext(DbContextOptions<GAAMSDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments { get; set; }

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(d => d.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EmployeeNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.NationalId)
                .IsUnique()
                .HasFilter("\"NationalId\" IS NOT NULL");

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}