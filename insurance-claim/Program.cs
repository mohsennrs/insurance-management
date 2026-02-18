using Microsoft.EntityFrameworkCore;
using insurance_claim.Data;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Register ClaimsDbContext
// builder.Services.AddDbContext<ClaimsDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
// Register ClaimsService for DI
builder.Services.AddScoped<insurance_claim.Services.IClaimsService, insurance_claim.Services.ClaimsService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Insurance Claims API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

app.MapOpenApi();           // serves /openapi/v1.json
app.MapScalarApiReference(); // serves /scalar/v1
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
