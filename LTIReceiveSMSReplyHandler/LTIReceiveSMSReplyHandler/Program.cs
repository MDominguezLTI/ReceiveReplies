using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using LTIReceiveSMSReplyHandler.Services.DataAccess;

var builder = FunctionsApplication.CreateBuilder(args);

// Load configuration from environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Retrieve database connection string from environment variables
var connectionString = builder.Configuration["Databases.Test_Customer_Portal"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("[Program.cs] Connection string is missing in environment variables.");
}

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContextFactory<DatabaseContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

// Register DatabaseServices
builder.Services.AddScoped<DatabaseServices>();

// Configure Function App
builder.ConfigureFunctionsWebApplication();

// Build and run the application
builder.Build().Run();
