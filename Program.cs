using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payroll.Data;
using Payroll.Models;
using Payroll.Services;
using System.Text;
using PayrollRecord = Payroll.Models.Payroll;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = Encoding.ASCII.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            ctx.Token = ctx.Request.Cookies["jwt"];
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        },
        OnForbidden = ctx =>
        {
            ctx.Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Seed default data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Roles.Any())
    {
        var superAdmin = new Role
        {
            Name        = "Super Admin",
            Description = "Full access to all modules",
            IsActive    = true
        };
        superAdmin.RolePermissions = Modules.All
            .Select(m => new RolePermission { Module = m.Key })
            .ToList();
        db.Roles.Add(superAdmin);
        db.SaveChanges();
    }
    else
    {
        // Ensure Super Admin has the BatchPayrollApproval module (added in workflow update)
        var superAdminRole = db.Roles.Include(r => r.RolePermissions).FirstOrDefault(r => r.Name == "Super Admin");
        if (superAdminRole != null && !superAdminRole.RolePermissions.Any(rp => rp.Module == Modules.BatchPayrollApproval))
        {
            superAdminRole.RolePermissions.Add(new RolePermission { RoleId = superAdminRole.Id, Module = Modules.BatchPayrollApproval });
            db.SaveChanges();
        }
    }

    if (!db.AdminUsers.Any())
    {
        var superAdminRole = db.Roles.First(r => r.Name == "Super Admin");
        var admin = new AdminUser
        {
            Username     = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd"),
            IsActive     = true
        };
        admin.AdminUserRoles = [new AdminUserRole { RoleId = superAdminRole.Id }];
        db.AdminUsers.Add(admin);
        db.SaveChanges();
    }
    else
    {
        // Ensure existing admin user has the Super Admin role
        var superAdminRole = db.Roles.FirstOrDefault(r => r.Name == "Super Admin");
        var admin = db.AdminUsers.Include(a => a.AdminUserRoles).FirstOrDefault(a => a.Username == "admin");
        if (superAdminRole != null && admin != null && !admin.AdminUserRoles.Any(ar => ar.RoleId == superAdminRole.Id))
        {
            admin.AdminUserRoles.Add(new AdminUserRole { AdminUserId = admin.Id, RoleId = superAdminRole.Id });
            db.SaveChanges();
        }
    }

    if (!db.PaymentSchemes.Any())
    {
        db.PaymentSchemes.AddRange(
            new PaymentScheme { Name = "Basic Grade",      BasicSalary = 1500.00m, OvertimeRatePerHour = 8.00m,  AllowanceAmount = 100.00m, IsActive = true },
            new PaymentScheme { Name = "Standard Monthly", BasicSalary = 3000.00m, OvertimeRatePerHour = 15.00m, AllowanceAmount = 300.00m, IsActive = true },
            new PaymentScheme { Name = "Senior Executive", BasicSalary = 6000.00m, OvertimeRatePerHour = 30.00m, AllowanceAmount = 800.00m, IsActive = true },
            new PaymentScheme { Name = "Part-Time",        BasicSalary = 800.00m,  OvertimeRatePerHour = 6.00m,  AllowanceAmount = 0.00m,   IsActive = true }
        );
        db.SaveChanges();
    }

    if (!db.Employees.Any())
    {
        var basicId    = db.PaymentSchemes.First(s => s.Name == "Basic Grade").Id;
        var standardId = db.PaymentSchemes.First(s => s.Name == "Standard Monthly").Id;
        var seniorId   = db.PaymentSchemes.First(s => s.Name == "Senior Executive").Id;
        var partTimeId = db.PaymentSchemes.First(s => s.Name == "Part-Time").Id;

        db.Employees.AddRange(
            new Employee { Name = "Alice Johnson",  Email = "alice.johnson@example.com",  Phone = "+1 555 100 0001", PaymentSchemeId = seniorId,   JoinDate = new DateTime(2021, 3, 15), IsActive = true  },
            new Employee { Name = "Bob Martinez",   Email = "bob.martinez@example.com",   Phone = "+1 555 100 0002", PaymentSchemeId = standardId, JoinDate = new DateTime(2022, 6, 1),  IsActive = true  },
            new Employee { Name = "Carol White",    Email = "carol.white@example.com",    Phone = "+1 555 100 0003", PaymentSchemeId = standardId, JoinDate = new DateTime(2022, 9, 20), IsActive = true  },
            new Employee { Name = "David Lee",      Email = "david.lee@example.com",      Phone = "+1 555 100 0004", PaymentSchemeId = basicId,    JoinDate = new DateTime(2023, 1, 10), IsActive = true  },
            new Employee { Name = "Eva Nguyen",     Email = "eva.nguyen@example.com",     Phone = "+1 555 100 0005", PaymentSchemeId = partTimeId, JoinDate = new DateTime(2023, 7, 5),  IsActive = true  },
            new Employee { Name = "Frank Brown",    Email = "frank.brown@example.com",    Phone = null,              PaymentSchemeId = basicId,    JoinDate = new DateTime(2024, 2, 28), IsActive = false }
        );
        db.SaveChanges();
    }

    // Batches must be seeded before payrolls so FK ids are available
    if (!db.PayrollBatches.Any())
    {
        db.PayrollBatches.AddRange(
            new PayrollBatch
            {
                Year = 2026, Month = 1,
                Description = "January 2026 Monthly Payroll",
                Status = PayrollBatchStatus.Completed,
                TotalEmployees = 5,
                TotalGrossSalary = 16231.00m,
                TotalDeductions  = 1581.30m,
                TotalNetSalary   = 14649.70m,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ProcessedAt = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            new PayrollBatch
            {
                Year = 2026, Month = 2,
                Description = "February 2026 Monthly Payroll",
                Status = PayrollBatchStatus.Completed,
                TotalEmployees = 5,
                TotalGrossSalary = 16132.00m,
                TotalDeductions  = 1570.20m,
                TotalNetSalary   = 14561.80m,
                CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ProcessedAt = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc)
            },
            new PayrollBatch
            {
                Year = 2026, Month = 3,
                Description = "March 2026 Monthly Payroll",
                Status = PayrollBatchStatus.Draft,
                TotalEmployees = 5,
                TotalGrossSalary = 16184.00m,
                TotalDeductions  = 1577.20m,
                TotalNetSalary   = 14606.80m,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        db.SaveChanges();
    }

    if (!db.Payrolls.Any())
    {
        var janBatch = db.PayrollBatches.First(b => b.Year == 2026 && b.Month == 1);
        var febBatch = db.PayrollBatches.First(b => b.Year == 2026 && b.Month == 2);
        var marBatch = db.PayrollBatches.First(b => b.Year == 2026 && b.Month == 3);

        var alice = db.Employees.First(e => e.Name == "Alice Johnson");
        var bob   = db.Employees.First(e => e.Name == "Bob Martinez");
        var carol = db.Employees.First(e => e.Name == "Carol White");
        var david = db.Employees.First(e => e.Name == "David Lee");
        var eva   = db.Employees.First(e => e.Name == "Eva Nguyen");

        static PayrollRecord Make(int employeeId, int batchId, DateTime start, DateTime end,
            decimal basic, decimal otHours, decimal otRate, decimal allowance,
            decimal deductions, PayrollStatus status)
        {
            var otPay = Math.Round(otHours * otRate, 2);
            var gross = basic + otPay + allowance;
            return new PayrollRecord
            {
                EmployeeId = employeeId, PayrollBatchId = batchId,
                PayPeriodStart = start, PayPeriodEnd = end,
                BasicSalary = basic, OvertimeHours = otHours, OvertimePay = otPay,
                Allowances = allowance, Deductions = deductions,
                GrossSalary = gross, NetSalary = gross - deductions,
                Status = status, CreatedAt = DateTime.UtcNow
            };
        }

        // Jan 2026 — Paid
        db.Payrolls.AddRange(
            Make(alice.Id, janBatch.Id, new(2026,1,1), new(2026,1,31), 6000, 5,  30, 800, 695.00m,  PayrollStatus.Paid),
            Make(bob.Id,   janBatch.Id, new(2026,1,1), new(2026,1,31), 3000, 8,  15, 300, 342.00m,  PayrollStatus.Paid),
            Make(carol.Id, janBatch.Id, new(2026,1,1), new(2026,1,31), 3000, 3,  15, 300, 334.50m,  PayrollStatus.Paid),
            Make(david.Id, janBatch.Id, new(2026,1,1), new(2026,1,31), 1500, 10, 8,  100, 168.00m,  PayrollStatus.Paid),
            Make(eva.Id,   janBatch.Id, new(2026,1,1), new(2026,1,31), 800,  6,  6,  0,   41.80m,   PayrollStatus.Paid)
        );

        // Feb 2026 — Approved
        db.Payrolls.AddRange(
            Make(alice.Id, febBatch.Id, new(2026,2,1), new(2026,2,28), 6000, 2,  30, 800, 686.00m,  PayrollStatus.Approved),
            Make(bob.Id,   febBatch.Id, new(2026,2,1), new(2026,2,28), 3000, 12, 15, 300, 348.00m,  PayrollStatus.Approved),
            Make(carol.Id, febBatch.Id, new(2026,2,1), new(2026,2,28), 3000, 0,  15, 300, 330.00m,  PayrollStatus.Approved),
            Make(david.Id, febBatch.Id, new(2026,2,1), new(2026,2,28), 1500, 4,  8,  100, 163.20m,  PayrollStatus.Approved),
            Make(eva.Id,   febBatch.Id, new(2026,2,1), new(2026,2,28), 800,  10, 6,  0,   43.00m,   PayrollStatus.Approved)
        );

        // Mar 2026 — Draft
        db.Payrolls.AddRange(
            Make(alice.Id, marBatch.Id, new(2026,3,1), new(2026,3,31), 6000, 6,  30, 800, 698.00m,  PayrollStatus.Draft),
            Make(bob.Id,   marBatch.Id, new(2026,3,1), new(2026,3,31), 3000, 5,  15, 300, 337.50m,  PayrollStatus.Draft),
            Make(carol.Id, marBatch.Id, new(2026,3,1), new(2026,3,31), 3000, 7,  15, 300, 340.50m,  PayrollStatus.Draft),
            Make(david.Id, marBatch.Id, new(2026,3,1), new(2026,3,31), 1500, 0,  8,  100, 160.00m,  PayrollStatus.Draft),
            Make(eva.Id,   marBatch.Id, new(2026,3,1), new(2026,3,31), 800,  4,  6,  0,   41.20m,   PayrollStatus.Draft)
        );

        db.SaveChanges();
    }
    else
    {
        // Fix any existing payrolls that were seeded without a batch link
        var batches   = db.PayrollBatches.ToList();
        var unlinked  = db.Payrolls.Where(p => p.PayrollBatchId == null).ToList();
        foreach (var p in unlinked)
        {
            var batch = batches.FirstOrDefault(b => b.Year == p.PayPeriodStart.Year && b.Month == p.PayPeriodStart.Month);
            if (batch != null) p.PayrollBatchId = batch.Id;
        }
        if (unlinked.Any(p => p.PayrollBatchId != null)) db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
