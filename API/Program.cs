using System.Reflection;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth.AspNetCore3;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
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
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Add the Services defined in infrastructure
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAlertService, AlertService>();

builder.Services.AddHttpClient("WaitAndRetry")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            3, retryNumber => TimeSpan.FromMilliseconds(600)));

// Add environment variables
builder.Configuration
    .AddJsonFile("appsettings.azure.json", false)
    .AddJsonFile("appsettings.json", false)
    .AddEnvironmentVariables()
    .Build();

// Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"])),
            RequireExpirationTime = true
        };
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.SlidingExpiration = true;
        options.LoginPath = new PathString("/authentication/login");
        options.LogoutPath = new PathString("/authentication/logout");
    })
    .AddOpenIdConnect("MicrosoftAuth", options =>
    {
        options.Authority = "https://login.microsoftonline.com/common/v2.0";
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ??
                           throw new InvalidOperationException("Microsoft Client ID missing");
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ??
                               throw new InvalidOperationException("Microsoft Client Secret missing");
        
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.CallbackPath = new PathString("/signin-oidc");
        
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(
            options.Authority).Validate;
    })
    .AddGoogleOpenIdConnect(GoogleOpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ??
                           throw new InvalidOperationException("Google Client ID missing");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ??
                               throw new InvalidOperationException("Google Client Secret missing");
        
        options.CallbackPath = new PathString("/signin-google");
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ShouldBeAllowedEmail", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        
        var validEmails = builder
            .Configuration
            .GetSection("AllowedEmails")
            .Get<string []>();
        
        if (validEmails == null || validEmails.Length == 0)
        {
            throw new InvalidConfigurationException("No valid emails found in configuration");
        }
        
        policy.RequireAssertion(context =>
        {
            var email = context.User.FindFirst(ClaimTypes.Email);
            return email != null && validEmails.Contains(email.Value);
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
