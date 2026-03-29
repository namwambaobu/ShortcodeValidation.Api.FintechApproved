using Microsoft.EntityFrameworkCore;
using Serilog;
using ShortcodeValidation.Api.Infrastructure.Database;

// ✅ Configure Serilog FIRST (before builder)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ✅ Plug Serilog into ASP.NET pipeline
builder.Host.UseSerilog();
builder.Services.AddHostedService<TransactionProcessor>();
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<TransactionConsumer>();
// ✅ Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Validate connection string EARLY (fail fast)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("❌ Database connection string is NOT configured. Check appsettings.json");
}

// ✅ Register PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<FundTransferClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// ✅ Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Endpoint
app.MapPost("/api/callback/mpesa", HandleMpesaCallback.Handle);

app.UseHttpsRedirection();

app.Run();