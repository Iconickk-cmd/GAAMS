
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

    public DbSet<LeaveRequest> LeaveRequests { get; set; }

    public DbSet<LeaveEntitlement> LeaveEntitlements { get; set; }

    public DbSet<LeaveBalance> LeaveBalances { get; set; }

    public DbSet<Position> Positions { get; set; }

    public DbSet<EmployeePositionHistory> EmployeePositionHistories { get; set; }

    public DbSet<EmployeeSeparation> EmployeeSeparations { get; set; }

    public DbSet<EmployeeSeparationClearance> EmployeeSeparationClearances { get; set; }

    public DbSet<EmployeeSeparationClearanceAudit>
        EmployeeSeparationClearanceAudits
    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DEPARTMENT CONFIGURATION
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

        // EMPLOYEE CONFIGURATION
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

            entity.Property(e => e.DateOfBirth)
                .HasColumnType("date");

            entity.Property(e => e.DateOfEmployment)
                .HasColumnType("date");

            entity.HasIndex(e => e.NationalId)
                .IsUnique()
                .HasFilter("\"NationalId\" IS NOT NULL");

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE REQUEST CONFIGURATION
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.LeaveType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(l => l.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(l => l.Status)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.Property(l => l.StartDate)
                .HasColumnType("date");

            entity.Property(l => l.EndDate)
                .HasColumnType("date");

            entity.Property(l => l.ReviewComments)
                .HasMaxLength(1000);

            entity.HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.ReviewedByEmployee)
                .WithMany()
                .HasForeignKey(l => l.ReviewedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE ENTITLEMENT CONFIGURATION
        modelBuilder.Entity<LeaveEntitlement>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LeaveType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EntitlementDays)
                .HasPrecision(8, 2);

            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("date");

            entity.Property(e => e.EffectiveTo)
                .HasColumnType("date");

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new
            {
                e.EmployeeId,
                e.LeaveType,
                e.LeaveYear
            })
            .IsUnique()
            .HasDatabaseName("IX_LeaveEntitlements_Employee_LeaveType_Year");

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_LeaveEntitlements_EntitlementDays",
                    "\"EntitlementDays\" >= 0"));
        });

        // LEAVE BALANCE CONFIGURATION
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.LeaveType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(b => b.EntitledDays)
                .HasPrecision(8, 2);

            entity.Property(b => b.AccruedDays)
                .HasPrecision(8, 2);

            entity.Property(b => b.TakenDays)
                .HasPrecision(8, 2);

            entity.Property(b => b.PendingDays)
                .HasPrecision(8, 2);

            entity.Property(b => b.RemainingDays)
                .HasPrecision(8, 2);

            entity.HasOne(b => b.Employee)
                .WithMany()
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new
            {
                b.EmployeeId,
                b.LeaveType,
                b.LeaveYear
            })
            .IsUnique()
            .HasDatabaseName("IX_LeaveBalances_Employee_LeaveType_Year");

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_LeaveBalances_NonNegativeDays",
                    "\"EntitledDays\" >= 0 AND " +
                    "\"AccruedDays\" >= 0 AND " +
                    "\"TakenDays\" >= 0 AND " +
                    "\"PendingDays\" >= 0 AND " +
                    "\"RemainingDays\" >= 0"));
        });

        // POSITION CONFIGURATION
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PositionCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(p => p.PositionCode)
                .IsUnique();

            entity.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.EmploymentType)
                .HasMaxLength(50);

            entity.Property(p => p.EmploymentGrade)
                .HasMaxLength(50);

            entity.HasOne(p => p.Department)
                .WithMany()
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Positions_ApprovedHeadcount",
                    "\"ApprovedHeadcount\" > 0"));
        });

        // EMPLOYEE POSITION HISTORY CONFIGURATION
        modelBuilder.Entity<EmployeePositionHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.ChangeReason)
                .HasMaxLength(500);

            entity.Property(h => h.StartDate)
                .HasColumnType("date");

            entity.Property(h => h.EndDate)
                .HasColumnType("date");

            entity.HasOne(h => h.Employee)
                .WithMany()
                .HasForeignKey(h => h.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Position)
                .WithMany()
                .HasForeignKey(h => h.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(h => new { h.EmployeeId, h.IsCurrent })
                .HasDatabaseName("IX_EmployeePositionHistories_EmployeeId_IsCurrent");

            entity.HasIndex(h => new { h.PositionId, h.IsCurrent })
                .HasDatabaseName("IX_EmployeePositionHistories_PositionId_IsCurrent");

            entity.HasIndex(h => h.EmployeeId)
                .IsUnique()
                .HasFilter("\"IsCurrent\" = TRUE")
                .HasDatabaseName("IX_EmployeePositionHistories_OneCurrentPosition");
        });

        // EMPLOYEE SEPARATION CONFIGURATION
        modelBuilder.Entity<EmployeeSeparation>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.SeparationType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("PendingApproval");

            entity.Property(s => s.RequestedBy)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(s => s.DecisionBy)
                .HasMaxLength(150);

            entity.Property(s => s.DecisionComments)
                .HasMaxLength(1000);

            entity.Property(s => s.FinalizedBy)
                .HasMaxLength(150);

            entity.Property(s => s.ProposedLastWorkingDate)
                .HasColumnType("date");

            entity.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.EmployeeId)
                .HasDatabaseName("IX_EmployeeSeparations_EmployeeId");

            entity.HasIndex(s => s.EmployeeId)
                .IsUnique()
                .HasFilter(
                    "\"Status\" IN ('PendingApproval', 'Approved', 'PendingClearance')")
                .HasDatabaseName("IX_EmployeeSeparations_OneActiveRequest");
        });

        // EMPLOYEE SEPARATION CLEARANCE CONFIGURATION
        modelBuilder.Entity<EmployeeSeparationClearance>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.DepartmentName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Status)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.Property(c => c.Remarks)
                .HasMaxLength(1000);

            entity.Property(c => c.ClearedBy)
                .HasMaxLength(150);

            entity.HasOne(c => c.EmployeeSeparation)
                .WithMany(s => s.Clearances)
                .HasForeignKey(c => c.EmployeeSeparationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.EmployeeSeparationId)
                .HasDatabaseName("IX_EmployeeSeparationClearances_SeparationId");
        });

        // EMPLOYEE SEPARATION CLEARANCE AUDIT CONFIGURATION
        modelBuilder.Entity<EmployeeSeparationClearanceAudit>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.PreviousClearedBy)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(a => a.PreviousRemarks)
                .HasMaxLength(1000);

            entity.Property(a => a.CorrectedClearedBy)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(a => a.CorrectedRemarks)
                .HasMaxLength(1000);

            entity.Property(a => a.CorrectionReason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(a => a.CorrectedBy)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasOne<EmployeeSeparationClearance>()
                .WithMany()
                .HasForeignKey(a => a.EmployeeSeparationClearanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.EmployeeSeparationClearanceId)
                .HasDatabaseName(
                    "IX_EmployeeSeparationClearanceAudits_ClearanceId");
        });
    }
}
