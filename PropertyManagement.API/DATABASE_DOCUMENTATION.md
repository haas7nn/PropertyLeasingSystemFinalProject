# Property Management Database - Role A Documentation

## Completed By
**Name:** Hasan Fadan
**Role:** A - Database + Entity Layer + Seed Data  
**Date:** 21/4/2026

## Database Overview

### Total Entities: 9
1. Building
2. Unit
3. Tenant (extends IdentityUser)
4. Lease
5. Payment
6. MaintenanceStaff (extends IdentityUser)
7. MaintenanceRequest
8. PropertyManager (extends IdentityUser)
9. Notification

### Total Tables: 16
- Application Tables: 9
- Identity Tables: 7

## Entity Relationships
Building (1) ──→ (∞) Unit
Unit (1) ──→ (∞) Lease (historical leases)
Unit (1) ──→ (0..1) Lease (current active lease)
Tenant (1) ──→ (∞) Lease
Lease (1) ──→ (∞) Payment
Tenant (1) ──→ (∞) MaintenanceRequest
Unit (1) ──→ (∞) MaintenanceRequest
MaintenanceStaff (1) ──→ (∞) MaintenanceRequest (assigned)


## Key Design Decisions

1. **CurrentLeaseId in Unit**: Allows tracking which lease is currently active
2. **DeleteBehavior.Restrict**: Prevents accidental data loss from cascade deletes
3. **Nullable Navigation Properties**: Used where relationships are optional
4. **TicketNumber Uniqueness**: Ensures no duplicate maintenance tickets
5. **Identity Integration**: Tenant, Staff, and Manager extend IdentityUser for authentication

## Seed Data Summary

### Users (6 total)
- **Property Manager (1):**
  - Email: manager@property.com
  - Password: Manager@123

- **Tenants (3):**
  - ali@tenant.com / Tenant@123 (Engineer, CPR: 920101111)
  - fatima@tenant.com / Tenant@123 (Teacher, CPR: 930202222)
  - sara@tenant.com / Tenant@123 (Doctor, CPR: 940303333)

- **Maintenance Staff (2):**
  - ahmed@staff.com / Staff@123 (Skills: Plumbing, Electrical)
  - mohammed@staff.com / Staff@123 (Skills: HVAC, Structural)

### Buildings (2)
1. **Sunset Tower** (Manama) - 5 residential units
2. **Marina Heights** (Seef) - 3 commercial units

### Units (8 total)
- 5 Apartments (1-3 bedrooms)
- 2 Offices
- 1 Shop

### Sample Data
- 1 Active Lease (Ali Ahmed → Unit 101)
- 3 Payment records (1 paid, 2 pending)
- 1 Maintenance Request (plumbing issue)

## Connection Strings

### Local Development
Server=(localdb)\mssqllocaldb;Database=PropertyManagementDB;Trusted_Connection=True;MultipleActiveResultSets=true


### Azure Production (To be configured)
Server=tcp:YOURSERVER.database.windows.net,1433;Initial Catalog=PropertyManagementDB;...


## Validation & Testing

### Test Endpoints
- GET /api/test/database-check - Verifies all entities have data
- GET /api/test/relationships-check - Verifies all relationships work
- GET /api/test/sample-data - Returns sample buildings, tenants, staff

### Test Results (All Passing)
```json
{
  "status": "✅ Database Connected Successfully",
  "buildings": 2,
  "units": 8,
  "tenants": 3,
  "maintenanceStaff": 2,
  "propertyManagers": 1,
  "leases": 1,
  "payments": 3,
  "maintenanceRequests": 1
}

## Files Delivered
Entity Models (/Models)
Building.cs
Unit.cs
Tenant.cs
Lease.cs
Payment.cs
MaintenanceStaff.cs
MaintenanceRequest.cs
PropertyManager.cs
Notification.cs
Data Layer (/Data)
ApplicationDbContext.cs
SeedData.cs
Testing (/Controllers)
TestController.cs

### Technical Notes
Framework: .NET 10 (LTS)
EF Core Version: 9.0.0
Database: SQL Server LocalDB
Authentication: ASP.NET Core Identity with JWT (to be implemented)
