using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CYHCare.Api.BackgroundServices;
using CYHCare.Api.Hubs;
using CYHCare.Api.Middleware;
using CYHCare.Api.Services;
using CYHCare.Application;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. High-Speed Response Compression (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// 2. High-Performance In-Memory Cache
builder.Services.AddMemoryCache();

// 3. Add Application and Infrastructure Dependency Injection layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register real-time SignalR notification dispatcher
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// Register scheduled background GPS telemetry data retention service (Phase 9)
builder.Services.AddHostedService<GpsRetentionBackgroundService>();

// 4. Add SignalR with options
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// 5. Configure CORS (allow Blazor Web and Hybrid apps)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Rate Limiting Protection (Phase 9 Hardening)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 150,
                QueueLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 7. Configure JWT Authentication (with SignalR query string token support)
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? "CYHCare_SuperSecretKey_For_Jwt_Auth_2026_Secure_Min32Chars!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "CYHCare.Api";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "CYHCare.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in strict production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Extract access_token from query parameter for SignalR WebSocket handshake
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 8. Controllers
builder.Services.AddControllers();

// 9. Swagger with JWT Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CYH Care API",
        Version = "v1",
        Description = "ASP.NET Core Web API, SignalR Tracking & Privacy Retention for CYH Care (People Help People)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

var app = builder.Build();

// Enable Response Compression at the start of pipeline
app.UseResponseCompression();

// Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CYH Care API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

// Rate Limiter
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 10. Map SignalR Tracking Hub
app.MapHub<TrackingHub>("/hubs/tracking");

app.Run();
