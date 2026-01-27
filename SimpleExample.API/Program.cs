using Microsoft.EntityFrameworkCore;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Infrastructure.Data;
using SimpleExample.Infrastructure.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Check if using in-memory database or real SQL Server
bool useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

if (useInMemoryDatabase)
{
    // Use in-memory repository (no database required)
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
    Console.WriteLine("✓ Using IN-MEMORY database with sample data");
}
else
{
    // Configure Entity Framework Core with SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    
    // Register repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    Console.WriteLine("✓ Using SQL SERVER database");
}

// Register services
builder.Services.AddScoped<IUserService, UserService>();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Apply database migrations (only if using real database)
if (!useInMemoryDatabase && app.Environment.IsDevelopment())
{
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
