using ConnectGrow.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
 
var apiOptions = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>()
    ?? throw new InvalidOperationException("The Api configuration section is missing.");
 
if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
    throw new InvalidOperationException("Api:BaseUrl must be configured.");
 
// The client authenticates against the API and keeps the
// resulting tokens inside its own encrypted auth cookie.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "csg.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
 
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
 
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
 
builder.Services.AddAuthorization();
 
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISignInService, SignInService>();
builder.Services.AddTransient<BearerTokenHandler>();

builder.Services
    .AddHttpClient<IAdminClient, AdminWebinarApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
 

 //https://postsharp.net/blog/polly 
 //https://www.c-sharpcorner.com/article/build-robust-middleware-in-net-retry-and-circuit-breaker-with-polly-v8

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(2, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
 
builder.Services
    .AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
    })
    .AddPolicyHandler(retryPolicy);
 
builder.Services
    .AddHttpClient<IWebinarApiClient, WebinarApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddPolicyHandler(retryPolicy);
 
builder.Services
    .AddHttpClient<IBookingApiClient, BookingApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
 
builder.Services.AddControllersWithViews();
 
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
 
var app = builder.Build();
 
 
app.UseForwardedHeaders();
 
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}
 

app.UseStaticFiles();
app.UseRouting();
 
app.UseAuthentication();
 
app.UseTokenRefresh();
 
app.UseAuthorization();


app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
 
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
 

app.Run();
