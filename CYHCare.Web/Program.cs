using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using CYHCare.Application;
using CYHCare.Infrastructure;
using CYHCare.Web.Components;
using CYHCare.Web.Services;

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

// 2. High-Performance In-Memory Cache
builder.Services.AddMemoryCache();

// 3. Clean Architecture Dependency Injection Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Session and HTTP Client
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddHttpClient();

// 4. Blazor Interactive Server with optimized options
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
        options.MaxBufferedUnacknowledgedRenderBatches = 15;
    });

var app = builder.Build();

// Enable Response Compression early
app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// 5. Static Files with 1-Year Browser Cache Headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 1 year
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={durationInSeconds}";
    }
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
