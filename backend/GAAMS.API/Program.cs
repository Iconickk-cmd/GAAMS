
using GAAMS.Application.Interfaces;
using GAAMS.Infrastructure.Data;
using GAAMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Register PostgreSQL database context
builder.Services.AddDbContext<GAAMSDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("GAAMSDatabase")
    )
);

// Register Leave Balance Service
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();

// Register OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("GAAMSFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapOpenApi();
}

app.UseCors("GAAMSFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
