using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Serilog;
using ShareService.Inject;
using ShareService.Models.Setting;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Eduflex.Authorization;
using Eduflex.API.BackgroundServices;
using Eduflex.API.Hubs;
using Eduflex.API.Realtime;
using ShareService.Services.Interface;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Needed by ICurrentUserService to read the acting user's id for audit stamping (CreatedBy/UpdatedBy)
builder.Services.AddHttpContextAccessor();

// ✅ Register Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();

// ✅ Register memory cache
builder.Services.AddMemoryCache();

// Redis distributed cache disabled (cost) — see project memory. Swapped for the in-box
// in-memory distributed cache so IDistributedCache still resolves for CoursePromotionService;
// functionally identical for a single-instance deployment, just not shared across instances.
// Uncomment below and drop AddDistributedMemoryCache() to restore Redis-backed caching.
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
//     options.InstanceName = "eduflex:";
// });
builder.Services.AddDistributedMemoryCache();

// ✅ SignalR for pushing live notifications to connected clients
builder.Services.AddSignalR();

// NotificationPublisher pushes straight into this hub via the interface below — no
// message bus needed for a single backend instance. A Redis (or similar) relay would
// only earn its cost back if this ever ran as multiple horizontally-scaled instances.
builder.Services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();

// ✅ Register health checks
builder.Services.AddHealthChecks();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Month)
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Swagger for multiple API groups
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("auth", new OpenApiInfo { Title = "Eduflex Auth API", Version = "v1" });
    c.SwaggerDoc("public", new OpenApiInfo { Title = "Eduflex Public API", Version = "v1" });
    c.SwaggerDoc("app", new OpenApiInfo { Title = "Eduflex App API", Version = "v1" });
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase));
});

// Configure MongoDB Settings
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

// Configure reCAPTCHA Settings (used by EnquiriesController to verify "I'm not a robot")
builder.Services.Configure<RecaptchaSettings>(
    builder.Configuration.GetSection("Recaptcha"));

//Configure Azure Blob Storage Settings (used by FilesController)
builder.Services.Configure<AzureBlobSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));

//Configure Document Link Settings (expiring SAS link lifetime for emailed document attachments)
builder.Services.Configure<DocumentLinkSettings>(
    builder.Configuration.GetSection("DocumentLinkSettings"));

//Configure Email Settings (used by EmailService for Azure Communication Services)
builder.Services.Configure<AzureEmailSettings>(
    builder.Configuration.GetSection("AzureEmailSettings"));

//Configure Web URL Settings (frontend URLs used in emails, e.g. login/reset links)
builder.Services.Configure<WebURLSettings>(
    builder.Configuration.GetSection("WebURLSettings"));

//Configure Gemini Settings (used by AIService for Gemini API calls)
builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("Gemini"));

//Configure Groq Settings (fallback provider for ChatService when Gemini is rate-limited)
builder.Services.Configure<GroqSettings>(
    builder.Configuration.GetSection("Groq"));

//Configure OpenRouter Settings (second fallback provider when both Gemini and Groq are unavailable)
builder.Services.Configure<OpenRouterSettings>(
    builder.Configuration.GetSection("OpenRouter"));

// Register MongoDB Client (Singleton - recommended by MongoDB)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDBConnection");
    return new MongoClient(connectionString);
});

// Register MongoDB Database (Scoped)
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                .GetBytes(builder.Configuration.GetSection("JWT:Secret").Value)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Browsers can't set the Authorization header on WebSocket/SSE connections,
                // so the SignalR JS client relies on the cookie below instead now.
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                    return Task.CompletedTask;
                }

                // No Authorization header (the normal browser case now that the JWT lives
                // in an httpOnly cookie) — fall back to reading it from there.
                if (string.IsNullOrEmpty(context.Token) &&
                    context.HttpContext.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });
// Add Authorization services + our custom permission-based handlers
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(options.DefaultPolicy)
        .AddRequirements(new MustChangePasswordRequirement())
        .Build();
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MustChangePasswordAuthorizationHandler>();

// Add CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate limiting for public endpoints that call metered external APIs (e.g. Gemini chat)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ChatPolicy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many questions in a short time. Please wait a few minutes and try again.", token);
    };
});

// Register all shared services (DataAccess, Services, Validators)
builder.Services.AddSharedServices();

var app = builder.Build();

// NSwag middleware
//app.UseOpenApi();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/auth/swagger.json", "Auth API v1");
    c.SwaggerEndpoint("/swagger/public/swagger.json", "Public API v1");
    c.SwaggerEndpoint("/swagger/app/swagger.json", "App API v1");
    c.RoutePrefix = "swagger";
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Must run before CORS/auth — otherwise AuthorizationMiddleware sees no matched endpoint yet,
// context.Resource ends up being the raw HttpContext instead of the Endpoint, and every
// endpoint-metadata-based check (like our custom [SkipMustChangePasswordCheck] attribute) is
// silently unreachable no matter what's applied to a controller action.
app.UseRouting();

app.UseCors("AllowAngular");
app.UseRateLimiter();
// Lightweight CSRF guard: browsers can't attach a custom header to a cross-site
// request without a CORS preflight, and our CORS policy only allows the configured
// Angular origin(s) — so requiring this header on state-changing requests blocks
// simple cross-site form/img/fetch CSRF attempts against the auth cookies.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var isStateChanging = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
                           HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    if (isStateChanging &&
        context.Request.Path.StartsWithSegments("/api") &&
        !context.Request.Headers.ContainsKey("X-Eduflex-Csrf"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Missing CSRF header");
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapGet("/", () => "Eduflex API is running 🚀");
app.MapHealthChecks("/health");

app.Run();