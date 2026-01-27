using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WebAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WebAppContext")));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Read connection string and container name from environment variables
var blobConnectionString = builder.Configuration["AzureBlob:ConnectionString"];
var blobContainerName = builder.Configuration["AzureBlob:ContainerName"];

// Create BlobServiceClient and BlobContainerClient
var blobServiceClient = new BlobServiceClient(blobConnectionString);
var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

// Make sure container exists
await blobContainerClient.CreateIfNotExistsAsync();

// Register BlobContainerClient in DI
builder.Services.AddSingleton(blobContainerClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
