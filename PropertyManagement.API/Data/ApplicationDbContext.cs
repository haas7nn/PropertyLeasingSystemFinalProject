using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Data
{
    // shared EF Core DbContext used by the API and the MVC app through a reference
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // table of physical buildings managed by the property company
        public DbSet<Building> Buildings { get; set; }

        // table of rentable units inside buildings
        public DbSet<Unit> Units { get; set; }

        // table of lease applications and active agreements
        public DbSet<Lease> Leases { get; set; }

        // table of monthly rent payment installment records
        public DbSet<Payment> Payments { get; set; }

        // table of maintenance requests submitted by tenants
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

        // table of in-system notifications created by NotificationService
        public DbSet<Notification> Notifications { get; set; }

        // table of role A users who are tenants leasing units
        public DbSet<Tenant> Tenants { get; set; }

        // table of role B users who are maintenance technicians
        public DbSet<MaintenanceStaff> MaintenanceStaffs { get; set; }

        // table of role C users who are property managers with full system access
        public DbSet<PropertyManager> PropertyManagers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // suppress the pending model changes warning because migrations are applied
            // automatically at startup via context.Database.MigrateAsync in SeedData.Initialize
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // must be called first to set up Identity tables

            builder.Entity<Building>().HasKey(b => b.BuildingId);
            builder.Entity<Unit>().HasKey(u => u.UnitId);
            builder.Entity<Lease>().HasKey(l => l.LeaseId);
            builder.Entity<Payment>().HasKey(p => p.PaymentId);
            builder.Entity<MaintenanceRequest>().HasKey(m => m.RequestId);
            builder.Entity<Notification>().HasKey(n => n.NotificationId);

            builder.Entity<IdentityUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // a building cannot be deleted while it still has units attached
            // the MVC controller enforces this with a pre-check in DeleteConfirmed
            builder.Entity<Unit>()
                .HasOne(u => u.Building)
                .WithMany(b => b.Units)
                .HasForeignKey(u => u.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            // a unit can have many leases in its history including rejected and terminated ones
            builder.Entity<Lease>()
                .HasOne(l => l.Unit)
                .WithMany(u => u.LeaseHistory)
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // CurrentLeaseId points to the single active lease for fast availability lookups
            // SetNull means the pointer is cleared automatically if the lease is deleted
            builder.Entity<Unit>()
                .HasOne(u => u.CurrentLease)
                .WithOne()
                .HasForeignKey<Unit>(u => u.CurrentLeaseId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // tenants with lease history cannot be removed to preserve records
            builder.Entity<Lease>()
                .HasOne(l => l.Tenant)
                .WithMany(t => t.Leases)
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // payments are meaningless without their lease so cascade delete is appropriate
            // the MVC controller adds a business rule check to block deleting leases with payments
            builder.Entity<Payment>()
                .HasOne(p => p.Lease)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LeaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // restrict so tenant history is preserved even if the account is deactivated
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.MaintenanceRequests)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Unit)
                .WithMany(u => u.MaintenanceRequests)
                .HasForeignKey(m => m.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // if a staff member is deleted their assigned requests become unassigned not deleted
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.AssignedStaff)
                .WithMany(s => s.AssignedRequests)
                .HasForeignKey(m => m.AssignedStaffId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);


            builder.Entity<Notification>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ticket numbers must be unique so two requests cannot share the same reference
            builder.Entity<MaintenanceRequest>()
                .HasIndex(m => m.TicketNumber)
                .IsUnique();

            // seed the three application roles into the Roles table
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Tenant",           NormalizedName = "TENANT" },
                new IdentityRole { Id = "2", Name = "PropertyManager",  NormalizedName = "PROPERTYMANAGER" },
                new IdentityRole { Id = "3", Name = "MaintenanceStaff", NormalizedName = "MAINTENANCESTAFF" }
            );
        }
    }
}
