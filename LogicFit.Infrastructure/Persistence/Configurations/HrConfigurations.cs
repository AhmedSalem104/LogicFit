using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("EmployeeProfiles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode).HasMaxLength(50);
        builder.Property(e => e.JobTitle).HasMaxLength(200);
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.BaseSalary).HasPrecision(18, 2);
        builder.Property(e => e.HourlyRate).HasPrecision(18, 2);
        builder.Property(e => e.BankAccount).HasMaxLength(100);
        builder.Property(e => e.BankName).HasMaxLength(100);
        builder.Property(e => e.NationalId).HasMaxLength(50);
        builder.Property(e => e.EmergencyContactName).HasMaxLength(200);
        builder.Property(e => e.EmergencyContactPhone).HasMaxLength(50);
        builder.Property(e => e.Qualifications).HasMaxLength(1000);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.UserId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode }).IsUnique().HasFilter("[EmployeeCode] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeBranchConfiguration : IEntityTypeConfiguration<EmployeeBranch>
{
    public void Configure(EntityTypeBuilder<EmployeeBranch> builder)
    {
        builder.ToTable("EmployeeBranches");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.EmployeeId, e.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Employee).WithMany(e => e.Branches).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(20);

        builder.HasIndex(e => e.TenantId);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ShiftId);
        builder.HasIndex(e => new { e.EmployeeId, e.Date });

        builder.HasOne(e => e.Shift).WithMany().HasForeignKey(e => e.ShiftId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason).HasMaxLength(1000);
        builder.Property(e => e.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ReviewedBy).WithMany().HasForeignKey(e => e.ReviewedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("Commissions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.SourceAmount).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.EarnedDate);

        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PayrollItem).WithMany(p => p.Commissions).HasForeignKey(e => e.PayrollItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.ToTable("CommissionRules");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Value).HasPrecision(18, 2);
        builder.Property(e => e.MinAmount).HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.Role);

        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Year, e.Month, e.BranchId });

        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> builder)
    {
        builder.ToTable("PayrollItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BaseSalary).HasPrecision(18, 2);
        builder.Property(e => e.CommissionTotal).HasPrecision(18, 2);
        builder.Property(e => e.Bonus).HasPrecision(18, 2);
        builder.Property(e => e.Deductions).HasPrecision(18, 2);
        builder.Property(e => e.NetSalary).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.PayrollRunId);
        builder.HasIndex(e => e.EmployeeId);

        builder.HasOne(e => e.PayrollRun).WithMany(p => p.Items).HasForeignKey(e => e.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
