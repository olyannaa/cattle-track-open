using System.Globalization;
using CAT.EF;
using CAT.Events;
using CAT.Filters;
using CAT.Services;
using CAT.Services.Ai;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var basePath = AppContext.BaseDirectory;
    var xmlPath = Path.Combine(basePath, "CATAPI.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var connectionString = builder.Configuration.GetConnectionString("PostgresDB");

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<MinioS3Service>();
builder.Services.AddSingleton<UserActionQueue>();
builder.Services.Configure<AiAssistantOptions>(builder.Configuration.GetSection("AiAssistant"));
builder.Services.AddHttpClient<OpenAiCompatibleLlmClient>();
builder.Services.AddHttpClient<HttpAiAsrClient>();
builder.Services.AddSingleton<DisabledAiAgentLlmClient>();
builder.Services.AddSingleton<DisabledAiAsrClient>();
builder.Services.AddSingleton<IAiAgentLlmClient>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiAssistantOptions>>().Value;
    return options.Llm.Provider is "ollama" or "openai-compatible" or "local-http"
        ? sp.GetRequiredService<OpenAiCompatibleLlmClient>()
        : sp.GetRequiredService<DisabledAiAgentLlmClient>();
});
builder.Services.AddSingleton<IAiConstrainedOutputValidator, DefaultAiConstrainedOutputValidator>();
builder.Services.AddSingleton<IAiToolSchemaValidator, AiToolSchemaValidator>();
builder.Services.AddSingleton<IAiAsrClient>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiAssistantOptions>>().Value;
    return options.Asr.Provider is "local-http" or "whisper-http"
        ? sp.GetRequiredService<HttpAiAsrClient>()
        : sp.GetRequiredService<DisabledAiAsrClient>();
});
builder.Services.AddHostedService<UserActionBackgroundService>();

builder.Services.AddScoped<IUserActionService, UserActionService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<IAnimalCardService, AnimalCardService>();
builder.Services.AddScoped<ISpreadsheetService, XLSXService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IWeightsService, WeightsService>();
builder.Services.AddScoped<IFeedingService, FeedingService>();
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IStatisticsChartsService, StatisticsChartsService>();
builder.Services.AddScoped<IAiToolValidationDataSource, EfAiToolValidationDataSource>();
builder.Services.AddScoped<IAiToolValidator, AiToolValidator>();
builder.Services.AddScoped<IAiReadToolDataSource, EfAiReadToolDataSource>();
builder.Services.AddScoped<IAiWriteToolService, AiWriteToolService>();
builder.Services.AddScoped<IAiAuditService, AiAuditService>();
builder.Services.AddScoped<IAiToolExecutor, AiReadToolExecutor>();
builder.Services.AddScoped<IAiAgentLoop, AiAgentLoop>();
builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();

builder.Services.AddScoped<IAuthService, CookiesAuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDailyActionService, DailyActionService>();

builder.Services.AddScoped<FeedingExportService>();
builder.Services.AddScoped<OrgValidationFilter>();

builder.Services.AddSingleton<CustomCookieAuthenticationEvents>();
builder.Services.AddHostedService<FeedingPlanScheduler>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; 
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.EventsType = typeof(CustomCookieAuthenticationEvents);
    });

builder.Services.AddAuthorization();

var efLoggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddFilter(level => level >= LogLevel.Warning);
});

builder.Services.AddDbContextFactory<PostgresContext>(options =>
    options.UseNpgsql(connectionString)
           .UseLoggerFactory(efLoggerFactory));

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4173",
                "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisCacheOptions:Configuration"];
    options.InstanceName = builder.Configuration["RedisCacheOptions:InstanceName"];
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendCors");

app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
