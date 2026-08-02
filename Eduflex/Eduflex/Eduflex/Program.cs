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
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Needed by ICurrentUserService to read the acting user's id for audit stamping (CreatedBy/UpdatedBy)
builder.Services.AddHttpContextAccessor();

// ✅ Register Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();

// ✅ Register memory cache
builder.Services.AddMemoryCache();

// ✅ Register Redis as the distributed cache (feedback/course-promotion cache-aside)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "eduflex:";
});

// ✅ Register a shared Redis connection multiplexer for pub/sub (feeds the notification listener)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
    var multiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")!);

    // StackExchange.Redis auto-reconnects on its own — these are just visibility hooks so an
    // outage shows up in logs instead of silently degrading (cache calls fail open, pub/sub
    // messages stop arriving, with nothing telling you why).
    multiplexer.ConnectionFailed += (_, args) =>
        logger.LogError("Redis connection failed on {EndPoint}: {FailureType}", args.EndPoint, args.FailureType);

    multiplexer.ConnectionRestored += (_, args) =>
        logger.LogInformation("Redis connection restored on {EndPoint}", args.EndPoint);

    return multiplexer;
});

// ✅ SignalR for pushing live notifications to connected clients
builder.Services.AddSignalR();

// ✅ Background listener that subscribes to the notifications channel and fans out to SignalR groups
builder.Services.AddHostedService<NotificationListener>();

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

        // Browsers can't set the Authorization header on WebSocket/SSE connections, so the
        // SignalR JS client sends the JWT as an "access_token" query string param instead —
        // this reads it from there, but only for requests hitting a hub path.
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

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapGet("/", () => "Eduflex API is running 🚀");
app.MapHealthChecks("/health");

app.Run();