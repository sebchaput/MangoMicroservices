using Mango.Services.EmailAPI.Data;
using Mango.Services.EmailAPI.Extension;
using Mango.Services.EmailAPI.Messaging;
using Mango.Services.EmailAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Create DbContext
var serverVersion = new MySqlServerVersion(new Version(10, 10, 0));
builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), serverVersion)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors();
});

var optionBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionBuilder.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), serverVersion);
builder.Services.AddSingleton(new EmailService(optionBuilder.Options));

builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// To execute any pending migration at program startup
ApplyMigration();

app.UseAzureServiceBusConsumer();

app.Run();

/*
 * Apply pending migrations
 */
void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}
