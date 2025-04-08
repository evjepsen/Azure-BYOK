using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Google.Apis.Auth.AspNetCore3;
using Infrastructure;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.OpenApi.Models;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add rate limiter to avoid DoS and resource exhaustion
builder.Services.AddRateLimiter(options =>
{
    // Add a global rate limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Get the 'NameIdentifier' claim from the JWT token.
        // Should apply all providers (ISP)
        var userId = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        // Use a fallback value if the claim is not present.
        var partitionKey = string.IsNullOrWhiteSpace(userId) ? "no-name-identifier" : userId;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 240,      // dummy value
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
    
    // Handle rejections due to rate limiting
    options.OnRejected = async (context, cancellationToken) =>
    {
        // setup response
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        
        await context.HttpContext.Response.WriteAsync($"You have exceeded the rate limit. Please try again later", cancellationToken);
    };
});


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
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IKeyVaultManagementService, KeyVaultManagementService>();

// Polly http client factory
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

// Configure the JWT options
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Jwt))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure the Key Vault options
builder.Services.AddOptions<ApplicationOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationOptions.Application))
    .ValidateDataAnnotations()
    .ValidateOnStart();


// Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        // Get the jwt options from the configuration
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.Jwt)
            .Get<JwtOptions>() ?? throw new ResourceNotFoundException("JWT Options was not found");
        
        // Setup the JWT options
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)
            ),
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
        // Get the microsoft options from the configuration
        var microsoftAuthOptions = builder.Configuration
            .GetSection(AuthenticationOptions.Microsoft)
            .Get<AuthenticationOptions>() ?? throw new ResourceNotFoundException("Microsoft Options was not found");
        
        // Set up the Microsoft options
        options.Authority = "https://login.microsoftonline.com/common/v2.0";
        options.ClientId = microsoftAuthOptions.ClientId;
        options.ClientSecret = microsoftAuthOptions.ClientSecret;
        
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
        // Get the Google options from the configuration
        var googleAuthOptions = builder.Configuration
            .GetSection(AuthenticationOptions.Google)
            .Get<AuthenticationOptions>() ?? throw new ResourceNotFoundException("Google Options was not found");
        
        // Set up the Google options
        options.ClientId = googleAuthOptions.ClientId;
        options.ClientSecret = googleAuthOptions.ClientSecret;
        options.CallbackPath = new PathString("/signin-google");
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ShouldBeAllowedEmail", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        
        var applicationOptions = builder.Configuration
            .GetSection(ApplicationOptions.Application)
            .Get<ApplicationOptions>() ?? throw new ResourceNotFoundException("Application options was not found");
        
        policy.RequireAssertion(context =>
        {
            var email = context.User.FindFirst(ClaimTypes.Email);
            return email != null && applicationOptions.AllowedEmails.Contains(email.Value);
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI( 
        // Removes the requirement for clicking the "Try it out" button in Swagger UI when using a endpoint
        options => options.EnableTryItOutByDefault()
        );
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter(); // important to add after UseAuthentication because the limiter uses auth info
app.UseAuthorization();

app.MapControllers();

app.Run();
