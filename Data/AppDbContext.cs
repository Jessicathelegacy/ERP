using Microsoft.EntityFrameworkCore;
using Payroll.Models;

namespace Payroll.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PaymentScheme> PaymentSchemes => Set<PaymentScheme>();
    public DbSet<Models.Payroll> Payrolls => Set<Models.Payroll>();
    public DbSet<PayrollBatch> PayrollBatches => Set<PayrollBatch>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PayrollBatchEvent> PayrollBatchEvents => Set<PayrollBatchEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email).IsUnique();

        modelBuilder.Entity<AdminUser>()
            .HasIndex(a => a.Username).IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name).IsUnique();

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.PaymentScheme)
            .WithMany(ps => ps.Employees)
            .HasForeignKey(e => e.PaymentSchemeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Payroll>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.Payrolls)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Payroll>()
            .HasOne(p => p.PayrollBatch)
            .WithMany(b => b.Payrolls)
            .HasForeignKey(p => p.PayrollBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Models.Payroll>()
            .HasIndex(p => new { p.PayrollBatchId, p.EmployeeId })
            .IsUnique()
            .HasFilter("[PayrollBatchId] IS NOT NULL");

        modelBuilder.Entity<AdminUserRole>()
            .HasKey(ar => new { ar.AdminUserId, ar.RoleId });

        modelBuilder.Entity<PayrollBatch>()
            .HasOne(b => b.SubmittedByAdminUser)
            .WithMany()
            .HasForeignKey(b => b.SubmittedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdminUserRole>()
            .HasOne(ar => ar.AdminUser)
            .WithMany(a => a.AdminUserRoles)
            .HasForeignKey(ar => ar.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdminUserRole>()
            .HasOne(ar => ar.Role)
            .WithMany(r => r.AdminUserRoles)
            .HasForeignKey(ar => ar.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.Module });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.RecipientAdminUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientAdminUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientAdminUserId, n.CreatedAt });

        modelBuilder.Entity<PayrollBatchEvent>()
            .HasOne(e => e.PayrollBatch)
            .WithMany(b => b.Events)
            .HasForeignKey(e => e.PayrollBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayrollBatchEvent>()
            .HasOne(e => e.AdminUser)
            .WithMany()
            .HasForeignKey(e => e.AdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PayrollBatchEvent>()
            .HasIndex(e => new { e.PayrollBatchId, e.OccurredAt });
    }
}
