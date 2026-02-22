using Microsoft.EntityFrameworkCore;
using document_management_service.Data;
using document_management_service.Services;
using Minio;
using Scalar.AspNetCore;
using Serilog;
using shared_messaging;
using shared_messaging.Events;
using shared_messaging.Services;

var builder = WebApplication.CreateBuilder(args);
// Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Add services to the container.
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DocumentsDbContext>(options =>
    options.UseNpgsql(connectionString));
// MinIO Client
var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin";
var minioUseSSL = builder.Configuration.GetValue<bool>("MinIO:UseSSL", false);

builder.Services.AddSingleton<IMinioClient>(sp =>
    new MinioClient()
        .WithEndpoint(minioEndpoint)
        .WithCredentials(minioAccessKey, minioSecretKey)
        .WithSSL(minioUseSSL)
        .Build());

// Application services
builder.Services.AddScoped<IStorageService, MinioStorageService>();
builder.Services.AddScoped<IDocumentsService, document_management_service.Services.DocumentsService>();

// Event Handlers
builder.Services.AddScoped<document_management_service.EventHandlers.ClaimCreatedEventHandler>();
builder.Services.AddScoped<document_management_service.EventHandlers.StoreDocumentRequestEventHandler>();

// RabbitMQ Event Bus (optional - check if connection string is configured)
var rabbitMQConnectionString = builder.Configuration["RabbitMQ:ConnectionString"];
if (!string.IsNullOrEmpty(rabbitMQConnectionString))
{
    builder.Services.AddRabbitMQEventBus(builder.Configuration, "claimflow-events");
    Log.Information("RabbitMQ event bus configured");
}
else
{
    Log.Warning("RabbitMQ:ConnectionString not configured - running without event bus");
}
// CORS
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── Database initialisation ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
    try
    {
        db.Database.EnsureCreated();
        Log.Information("Database initialised successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialise the database");
    }
}
var eventBus = app.Services.GetService<IEventBus>();
if (eventBus != null)
{
    eventBus.Subscribe<ClaimCreatedEvent, document_management_service.EventHandlers.ClaimCreatedEventHandler>();

    eventBus.Subscribe<StoreDocumentRequestEvent, document_management_service.EventHandlers.StoreDocumentRequestEventHandler>();
    Log.Information("Subscribed to Claims Service events");
}

app.Run();
