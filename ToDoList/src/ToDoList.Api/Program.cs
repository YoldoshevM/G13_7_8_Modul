using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using ToDoList.Api.Configurations;
using ToDoList.Api.Filters;
using ToDoList.Api.Middlewares;
using ToDoList.Application;
using ToDoList.Infrastructure;

namespace ToDoList.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---- Logging (Serilog) ----
        builder.Host.UseSerilog((context, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/todolist-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14));

        // ---- MVC / Controllers (with global validation filter) ----
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        // ---- Swagger / OpenAPI (with JWT bearer support) ----
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "ToDoList API", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter the JWT access token as: Bearer {your token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });

        // ---- CORS ----
        const string corsPolicy = "DefaultCorsPolicy";
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicy, policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        // ---- Rate limiting (applied to auth endpoints) ----
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueLimit = 0;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        // ---- Application / Infrastructure / API DI ----
        builder.ConfigureJwt();
        builder.Services.ConfigureInfrastructure(builder.Configuration);
        builder.Services.ConfigureApplication(builder.Configuration);
        builder.AddJwtAuthentication();
        builder.Services.ConfigureDI();

        var app = builder.Build();

        // ---- HTTP pipeline ----
        app.UseExceptionHandlingMiddleware();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment() || true)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors(corsPolicy);

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // ============================================================================
        // ONE-TIME SAMPLE DATA SEEDING
        // ----------------------------------------------------------------------------
        // Make sure the database schema exists first (run once in a terminal):
        //     dotnet ef database update --project src/ToDoList.Persistence \
        //         --startup-project src/ToDoList.Api
        //
        // Run the app ONCE with the line below enabled to insert 10 users and
        // 30 to-do items, then COMMENT OUT the line to avoid re-adding the data.
        // (It is also self-guarding: it skips seeding if users already exist.)
        // ============================================================================
        //await app.SeedSampleDataAsync();

        await app.RunAsync();
    }
}