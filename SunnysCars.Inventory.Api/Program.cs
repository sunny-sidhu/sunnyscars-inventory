using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

builder.Services.Configure<CarStoreOptions>(builder.Configuration.GetSection(CarStoreOptions.SectionName));
builder.Services.AddSingleton<ICarStore, JsonCarStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapCarEndpoints();

app.Run();
