using System.Reflection;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.OpenApi.Models;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Azure BYOK API",
        Description = "An ASP.NET Core Web API to enable BYOK for Azure Key Vault"
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Add the Services defined in infrastructure
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddHttpClient("WaitAndRetry")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            3, retryNumber => TimeSpan.FromMilliseconds(600)));

// Add environment variables
builder.Configuration
    .AddJsonFile("appsettings.azure.json", false, true)
    .AddEnvironmentVariables()
    .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
