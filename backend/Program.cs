using backend.Auth;
using backend.Data;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
const string DevelopmentCorsPolicy = "DevelopmentCors";

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IStoredFileRepository, StoredFileRepository>();
builder.Services.AddScoped<IStoredFolderRepository, StoredFolderRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStoredFileService, StoredFileService>();
builder.Services.AddScoped<IStoredFolderService, StoredFolderService>();
builder.Services.AddScoped<IStorageUsageService, StorageUsageService>();
builder.Services.AddScoped<ITrashCleanupService, TrashCleanupService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHashService, Pbkdf2PasswordHashService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddOptions<AzureBlobStorageOptions>()
    .Bind(builder.Configuration.GetRequiredSection(AzureBlobStorageOptions.SectionName));
builder.Services.AddOptions<StorageQuotaOptions>()
    .Bind(builder.Configuration.GetSection(StorageQuotaOptions.SectionName));
builder.Services.AddOptions<TrashCleanupOptions>()
    .Bind(builder.Configuration.GetSection(TrashCleanupOptions.SectionName));
builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetRequiredSection(AuthOptions.SectionName));
builder.Services.AddOptions<AdminUserOptions>()
    .Bind(builder.Configuration.GetRequiredSection(AdminUserOptions.SectionName));
builder.Services.AddOptions<HolloServerOptions>()
    .Bind(builder.Configuration.GetSection(HolloServerOptions.SectionName));
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
builder.Services.AddHostedService<TrashCleanupHostedService>();
builder.Services.AddAuthentication(HolloJwtDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(
        HolloJwtDefaults.AuthenticationScheme,
        options => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApplicationRole.Admin, policy => policy.RequireRole(ApplicationRole.Admin));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(IsDevelopmentOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hollo API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    environment = app.Environment.EnvironmentName,
    utcNow = DateTimeOffset.UtcNow
}));

app.MapGet("/health/db", async (backend.Data.ApplicationDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? Results.Ok(new { status = "Healthy", database = "Available", utcNow = DateTimeOffset.UtcNow })
            : Results.Problem("Database is not available.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.EnsureDefaultAdminAsync();
}

app.Run();

static bool IsDevelopmentOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (uri.Scheme is not ("http" or "https"))
    {
        return false;
    }

    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!IPAddress.TryParse(uri.Host, out var ipAddress))
    {
        return false;
    }

    if (IPAddress.IsLoopback(ipAddress))
    {
        return true;
    }

    var bytes = ipAddress.GetAddressBytes();

    return bytes.Length == 4 &&
        (bytes[0] == 10 ||
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168));
}
