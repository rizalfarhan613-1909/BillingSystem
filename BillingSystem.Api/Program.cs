using Azure.Storage.Queues;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Api.Validators;
using BillingSystem.Application;
using BillingSystem.Infrastructure.Queue;
using FluentValidation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Tambahkan service Controllers, CORS, Swagger, dan FluentValidation
builder.Services.AddControllers();

// Konfigurasi CORS untuk frontend Next.js (port 3000)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Automated Billing System API",
        Version = "v1",
        Description = "Automated Billing System API v1 - Sistem penagihan otomatis untuk manajemen langganan dan pembuatan invoice.",
        Contact = new OpenApiContact
        {
            Name = "Billing System Team",
            Email = "support@billing.local"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Konfigurasi Options pattern
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));

// Daftarkan Queue Publisher
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IQueuePublisher, AzureQueuePublisher>();

// Health checks: SQL Server + Azure Queue (Storage)
var storageConnectionString = builder.Configuration["Storage:ConnectionString"] ?? string.Empty;
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .AddAzureQueueStorage(_ => new QueueServiceClient(storageConnectionString));

// Daftarkan DbContext dengan SQL Server
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();

var app = builder.Build();

// Konfigurasi HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Automated Billing System API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Automated Billing System API";
    });
}

app.UseHttpsRedirection();

// Aktifkan middleware CORS (wajib berada sebelum UseAuthorization)
app.UseCors();

app.UseAuthorization();
app.MapControllers();

// Health probes (BR-30): liveness selalu OK jika proses hidup, readiness cek SQL + Queue.
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready");

app.Run();