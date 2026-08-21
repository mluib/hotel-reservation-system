
using System.Text.Json.Serialization;
using HotelReservation.Api.Middleware;
using HotelReservation.Api.Seed;
using HotelReservation.Application.Customers;
using HotelReservation.Application.Hotels;
using HotelReservation.Application.Interfaces;
using Microsoft.OpenApi.Models;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Rooms;
using HotelReservation.Infrastructure.Persistence;
using HotelReservation.Infrastructure.Repositories;
using HotelReservation.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HotelReservation.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Bootstrap logger: covers anything that goes wrong before the full pipeline
            // (configured from appsettings' "Serilog" section, wired up via UseSerilog
            // below) exists yet -- e.g. bad configuration or a host build failure.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                await RunAsync(args);
            }
            // WebApplicationFactory (used by HotelReservation.Tests.Integration) builds the
            // host by invoking this Main method and relies on a HostAbortedException
            // unwinding back out of it once the builder is captured, without ever calling
            // Run(). A catch-all here would otherwise swallow that and break the test host.
            catch (Exception ex) when (ex is not HostAbortedException)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task RunAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Replace the bootstrap logger with the fully configured one now that
            // builder.Configuration (appsettings + env) is available. Built eagerly from
            // Log.Logger rather than via UseSerilog's lazy (context, services, config)
            // overload: that overload wraps a ReloadableLogger which freezes on first use
            // and throws if the host is ever built a second time -- which is exactly what
            // WebApplicationFactory (HotelReservation.Tests.Integration) does internally.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
            builder.Host.UseSerilog();

            // Add services to the container.
            {
                builder.Services.AddControllers()
                    // Serialize enums (RoomType, ReservationStatus) as their names ("Single",
                    // "Confirmed") instead of the default underlying int, so API consumers
                    // don't have to hardcode numeric-to-name mappings.
                    .AddJsonOptions(options =>
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

                builder.Services.AddEndpointsApiExplorer();

                // CORS: allow the Angular dev server to call this API. Origins are read from
                // Cors:AllowedOrigins (set in appsettings.Development.json to localhost:4200)
                // so a Docker/staging origin can be added later without touching this code;
                // the literal below is only a fallback if that config section is ever missing.
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:4200" };
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("Frontend", policy =>
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod());
                });

                // OpenAPI
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                // URL: https://localhost:7290/openapi/v1.json
                // Attention: needs Microsoft.AspNetCore.OpenApi, which conflicts with Swashbuckle 6.5.0
                //builder.Services.AddOpenApi();

                // Swagger
                // URL: https://localhost:7290/swagger
                // Attention: newest version 10.2.3 of Swashbuckle can't be configured correctly, so use 6.5.0 instead
                builder.Services.AddSwaggerGen(c =>
                {
                    // Enable JWT authentication in Swagger
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
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

                    // Prerequisite: GenerateDocumentationFile must be turned on in each
                    // project's own .csproj, or its .xml simply won't exist here to load --
                    // currently set on both HotelReservation.Api.csproj and
                    // HotelReservation.Application.csproj.
                    //
                    // Api's own XML doc file (controllers/middleware) plus Application's
                    // (DTOs, exception types) -- both land in this project's output
                    // directory since Application is a ProjectReference, so its generated
                    // .xml is copied alongside its .dll the same way HotelReservation.Api.xml is.
                    foreach (var assemblyName in new[] { "HotelReservation.Api", "HotelReservation.Application" })
                    {
                        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
                        if (File.Exists(xmlPath))
                            c.IncludeXmlComments(xmlPath);
                    }
                });

                // Database
                builder.Services.AddDbContext<HotelDbContext>(options =>
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString("DefaultConnection")));

                // Authentication & Authorization
                {
                    // Identity
                    builder.Services.AddIdentity<Microsoft.AspNetCore.Identity.IdentityUser, Microsoft.AspNetCore.Identity.IdentityRole>()
                    .AddEntityFrameworkStores<HotelDbContext>();

                    // JWT Authentication. No fallback defaults here (previously a hardcoded
                    // placeholder key) -- missing config now fails fast and loud at startup
                    // instead of silently running with a weak, guessable key. Jwt:Key is a
                    // real secret (user-secrets locally, the committed .env for
                    // docker-compose); Issuer/Audience aren't secret and stay in
                    // appsettings.json, but are held to the same fail-fast standard for
                    // consistency.
                    var jwtKey = builder.Configuration["Jwt:Key"]
                        ?? throw new InvalidOperationException(
                            "Jwt:Key is not configured. Set it via 'dotnet user-secrets set \"Jwt:Key\" \"...\"' for native dev, or via the repo-root .env file for docker-compose.");
                    var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
                    var jwtAudience = builder.Configuration["Jwt:Audience"]
                        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

                    builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtIssuer,
                            ValidateAudience = true,
                            ValidAudience = jwtAudience,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Convert.FromBase64String(jwtKey))
                        };
                    });

                    builder.Services.AddAuthorization();

                    builder.Services.AddScoped<Application.Interfaces.IJwtTokenService, Infrastructure.Services.JwtTokenService>();
                    builder.Services.AddScoped<Application.Interfaces.IAuthService, Infrastructure.Services.AuthService>();
                }

                // Accessor for current user (used by application services to enforce ownership)
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICurrentUserService, Services.CurrentUserService>();

                // Reservation Use-Case
                builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
                builder.Services.AddScoped<CreateReservation>();
                builder.Services.AddScoped<GetReservations>();
                builder.Services.AddScoped<GetReservationById>();
                builder.Services.AddScoped<GetMyReservations>();
                builder.Services.AddScoped<DeleteReservation>();
                builder.Services.AddScoped<CancelReservation>();

                // Customer Use-Case
                builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
                builder.Services.AddScoped<GetCustomerById>();
                builder.Services.AddScoped<GetCustomers>();
                builder.Services.AddScoped<UpdateCustomer>();
                builder.Services.AddScoped<DeleteCustomer>();
                builder.Services.AddScoped<GetCurrentCustomer>();

                // Room Use-Case
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<CreateRoom>();
                builder.Services.AddScoped<GetRooms>();
                builder.Services.AddScoped<GetRoomById>();
                builder.Services.AddScoped<UpdateRoom>();
                builder.Services.AddScoped<DeleteRoom>();
                builder.Services.AddScoped<UploadRoomImage>();

                // Hotel Use-Case
                builder.Services.AddScoped<IHotelRepository, HotelRepository>();
                builder.Services.AddScoped<GetHotel>();
                builder.Services.AddScoped<UpdateHotel>();
                builder.Services.AddScoped<UploadHotelImage>();

                // Image storage (local disk, under wwwroot)
                var webRootPath = builder.Environment.WebRootPath
                    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
                builder.Services.AddSingleton<IImageStorageService>(
                    _ => new ImageStorageService(webRootPath));
            }

            var app = builder.Build();

            // Apply pending EF Core migrations automatically on startup, so a fresh
            // container (or any real SQL Server target) always has an up-to-date schema
            // without a separate manual migration step. Guarded by IsSqlServer(), not by
            // environment: HotelReservation.Tests.Integration's CustomWebApplicationFactory
            // also runs as "Development" but swaps in a SQLite provider for tests, so an
            // environment check alone wouldn't have excluded it — the provider check does.
            // also: migration anyway is only consistent for SQL Server
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                if (db.Database.IsSqlServer())
                {
                    await db.Database.MigrateAsync();
                }
            }

            // Configure the HTTP request pipeline.
            {
                // One structured line per request (method, path, status code, elapsed ms),
                // replacing the framework's own multi-line-per-request logging. Registered
                // first so it wraps every other middleware, including the exception handler
                // below, and reports the true final status code and duration.
                app.UseSerilogRequestLogging();

                // Catches anything no controller/use case already handles, logs it, and
                // returns a generic error instead of an unhandled 500 with no trace anywhere.
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                if (app.Environment.IsDevelopment())
                {
                    // OpenAPI
                    //app.MapOpenApi();

                    // Swagger
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseCors("Frontend");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
            }

            // Dev-only seed: without this there's no way to obtain an Admin account (since
            // registration always assigns "Customer" and there's no promotion mechanism yet)
            // or a populated hotel to browse. Seed:AdminEmail/AdminPassword come from
            // user-secrets (native dev) or the committed .env (docker-compose) rather than
            // committed plaintext appsettings -- see Phase 6's secrets-hardening stage.
            // Still explicitly dev-only: no production-appropriate seeding/promotion path
            // exists, and building one was a deliberate non-goal for this project (see the
            // workflow log) rather than an oversight. See HotelReservation.Api/Seed for what
            // each method does and why the two are kept separate.
            if (app.Environment.IsDevelopment())
            {
                await DevelopmentSeeder.SeedRolesAndAdminAsync(app);
                await DevelopmentSeeder.SeedDemoDataAsync(app);
            }

            await app.RunAsync();
        }
    }
}
