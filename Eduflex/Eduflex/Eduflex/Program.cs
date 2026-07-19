using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using ShareService.Inject;
using ShareService.Models.Setting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Register health checks
builder.Services.AddHealthChecks();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Month)
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("auth", new OpenApiInfo { Title = "Eduflex Auth API", Version = "v1" });
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

//Configure Feedback Settings (used by FeedbacksController)
builder.Services.Configure<FeedbackSettings>(
    builder.Configuration.GetSection("FeedbackSettings"));

//Configure Course Promotion Settings (used by CoursePromotionsController)
builder.Services.Configure<CoursePromotionSettings>(
    builder.Configuration.GetSection("CoursePromotionSettings"));

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
    });

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

app.MapGet("/", () => "Eduflex API is running 🚀");
app.MapHealthChecks("/health");

app.Run();