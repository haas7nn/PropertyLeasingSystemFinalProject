using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Lease> Leases { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Identity-based entities
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<MaintenanceStaff> MaintenanceStaffs { get; set; }
        public DbSet<PropertyManager> PropertyManagers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== EXPLICITLY DEFINE PRIMARY KEYS =====
            builder.Entity<Building>().HasKey(b => b.BuildingId);
            builder.Entity<Unit>().HasKey(u => u.UnitId);
            builder.Entity<Lease>().HasKey(l => l.LeaseId);
            builder.Entity<Payment>().HasKey(p => p.PaymentId);
            builder.Entity<MaintenanceRequest>().HasKey(m => m.RequestId);
            builder.Entity<Notification>().HasKey(n => n.NotificationId);

            // Configure Identity table names (optional, for cleaner database)
            builder.Entity<IdentityUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // ===== BUILDING - UNIT RELATIONSHIP =====
            builder.Entity<Unit>()
                .HasOne(u => u.Building)
                .WithMany(b => b.Units)
                .HasForeignKey(u => u.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== UNIT - LEASE RELATIONSHIP (Historical) =====
            builder.Entity<Lease>()
                .HasOne(l => l.Unit)
                .WithMany(u => u.LeaseHistory)
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== UNIT - CURRENT LEASE RELATIONSHIP (One-to-Zero-or-One) =====
            builder.Entity<Unit>()
                .HasOne(u => u.CurrentLease)
                .WithOne()
                .HasForeignKey<Unit>(u => u.CurrentLeaseId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ===== TENANT - LEASE RELATIONSHIP =====
            builder.Entity<Lease>()
                .HasOne(l => l.Tenant)
                .WithMany(t => t.Leases)
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== LEASE - PAYMENT RELATIONSHIP =====
            builder.Entity<Payment>()
                .HasOne(p => p.Lease)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LeaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== TENANT - MAINTENANCE REQUEST RELATIONSHIP =====
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.MaintenanceRequests)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== UNIT - MAINTENANCE REQUEST RELATIONSHIP =====
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Unit)
                .WithMany(u => u.MaintenanceRequests)
                .HasForeignKey(m => m.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== MAINTENANCE STAFF - MAINTENANCE REQUEST RELATIONSHIP =====
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.AssignedStaff)
                .WithMany(s => s.AssignedRequests)
                .HasForeignKey(m => m.AssignedStaffId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ===== UNIQUE CONSTRAINTS =====
            builder.Entity<MaintenanceRequest>()
                .HasIndex(m => m.TicketNumber)
                .IsUnique();

            // ===== SEED ROLES =====
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = "Tenant",
                    NormalizedName = "TENANT"
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = "PropertyManager",
                    NormalizedName = "PROPERTYMANAGER"
                },
                new IdentityRole
                {
                    Id = "3",
                    Name = "MaintenanceStaff",
                    NormalizedName = "MAINTENANCESTAFF"
                }
            );
        }
    }
}