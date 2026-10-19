using Azure.Storage.Queues;
using BillingSystem.Application;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Infrastructure.Queue;
using BillingSystem.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using HealthChecks.Azure.Storage.Queues;

var builder = Host.CreateApplicationBuilder(args);

// Konfigurasi Options pattern
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));

// Daftarkan DbContext
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Daftarkan Queue Publisher (untuk ScanRequestConsumer jika perlu)
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IQueuePublisher, AzureQueuePublisher>();

// Health checks untuk Worker (SQL Server + Azure Queue)
var storageConnectionString = builder.Configuration["Storage:ConnectionString"] ?? string.Empty;
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .AddAzureQueueStorage(_ => new QueueServiceClient(storageConnectionString));

// Daftarkan Background Services
builder.Services.AddHostedService<ScanRequestConsumer>();
builder.Services.AddHostedService<InvoiceJobConsumer>();
builder.Services.AddHostedService<OverdueSweeperService>();

var host = builder.Build();
host.Run();
