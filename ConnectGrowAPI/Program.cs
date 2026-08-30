using ConnectGrowAPI.Data;
using ConnectGrowAPI.Models;
using Microsoft.EntityFrameworkCore;


using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ConnectGrowAPI.Api.Controllers;
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Repositories;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.OpenApi;
using ConnectGrowAPI.Api.Data;
using Microsoft.OpenApi;
using ConnectGrowAPI.Services.Payments;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettingsDev.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration["CSG_DB"]
    ?? throw new InvalidOperationException(
        "Connection string is not configured.");
 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
 
        npgsql.CommandTimeout(30);
        
    })
    .UseSnakeCaseNamingConvention();
 
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

//addindentity services for cookie authneication
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;   // will enable once SendGrid is added
 
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
 
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()          // needed for CheckPasswordSignInAsync and lockout
    .AddDefaultTokenProviders(); // needed for password-reset tokens
 
// JWT authentication
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
 
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The Jwt configuration section is missing.");
    
var jwtKey = builder.Configuration["CSG_JwtKey"];
 
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt key must be set");
}
 
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
 
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
 
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
 
            ValidateLifetime = true,
 
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
 
        options.Events = new JwtBearerEvents
        {
            // Browser clients hold the token in an httpOnly cookie rather than in
            // JavaScript, so fall back to the cookie when no Authorization
            // header is present.
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    var fromCookie = context.Request.Cookies[AuthController.AccessTokenCookie];
                    if (!string.IsNullOrEmpty(fromCookie))
                        context.Token = fromCookie;
                }
 
                return Task.CompletedTask;
            },
 
            OnChallenge = context =>
            {
                if (context.AuthenticateFailure is SecurityTokenExpiredException)
                    context.Response.Headers.Append("x-token-expired", "true");
 
                return Task.CompletedTask;
            }
        };
    });
 
builder.Services.AddAuthorization();
 

// CORS
 
const string ClientCorsPolicy = "ClientCorsPolicy";
 
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();
 
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        // Credentialed requests cannot use a random origin, so every allowed
        // origin is listed explicitly in configuration
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("x-token-expired");
    });
});
 
 // Register application services
 
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IWebinarRepository, WebinarRepository>();
 
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IWebinarService, WebinarService>();

builder.Services.Configure<PayFastOptions>(
    builder.Configuration.GetSection(PayFastOptions.SectionName));
 
var payFastConfigured = !string.IsNullOrWhiteSpace(
    builder.Configuration[$"{PayFastOptions.SectionName}:MerchantId"]);
 
if (payFastConfigured)
{
    builder.Services.AddScoped<IPaymentService, PayFastPaymentService>();
}
else
{
    builder.Services.AddScoped<IPaymentService, NullPaymentService>();
}
 
builder.Services.AddScoped<IPayFastItnValidator, PayFastItnValidator>();
 
builder.Services.AddHttpClient(PayFastItnValidator.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "CSG-Platform/1.0");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//swagger for testing
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Connect Support Grow API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/auth/login."
    });
 /* 
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
    }); */
});

 
var app = builder.Build();
 

app.UseForwardedHeaders();
 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
 
app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);
 
app.UseAuthentication();
app.UseAuthorization();
 
app.MapControllers();
 
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }))
   .AllowAnonymous();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
 
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
 
        await DbSeeder.SeedAsync(services);
        logger.LogInformation("Database migrated and seeded.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration or seeding failed.");
        throw;
    }
}
 
app.Run();
 
public partial class Program { }
 