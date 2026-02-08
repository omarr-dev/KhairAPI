using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using KhairAPI.Data;
using KhairAPI.Core.Helpers;
using KhairAPI.Services.Interfaces;
using KhairAPI.Services.Implementations;
using KhairAPI.Services;
using Hangfire;
using Hangfire.PostgreSql;

namespace KhairAPI.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add database context with PostgreSQL
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        /// <summary>
        /// Add JWT authentication
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT Secret Key not configured"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppConstants.Policies.SupervisorOnly,
                    policy => policy.RequireRole(AppConstants.Roles.Supervisor));
                options.AddPolicy(AppConstants.Policies.TeacherOrSupervisor,
                    policy => policy.RequireRole(AppConstants.Roles.Teacher, AppConstants.Roles.Supervisor));
                // HalaqaSupervisor can do most supervisor tasks within their assigned halaqas
                options.AddPolicy(AppConstants.Policies.HalaqaSupervisorOrHigher,
                    policy => policy.RequireRole(AppConstants.Roles.Supervisor, AppConstants.Roles.HalaqaSupervisor));
                // Any authenticated user with a valid role
                options.AddPolicy(AppConstants.Policies.AnyRole,
                    policy => policy.RequireRole(AppConstants.Roles.Supervisor, AppConstants.Roles.HalaqaSupervisor, AppConstants.Roles.Teacher));
            });

            return services;
        }

        /// <summary>
        /// Add Swagger with JWT support
        /// </summary>
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Khair API - نظام إدارة الحلقات القرآنية",
                    Version = "v1",
                    Description = "API لنظام إدارة حلقات تحفيظ القرآن الكريم"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
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

            return services;
        }

        /// <summary>
        /// Add CORS for Next.js frontend
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("NextJsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            return services;
        }

        /// <summary>
        /// Add Hangfire with PostgreSQL storage
        /// </summary>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(action => action.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15)
                }));

            services.AddHangfireServer();

            return services;
        }

        /// <summary>
        /// Add application services
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core services
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Multi-tenancy
            services.AddScoped<ITenantService, TenantService>();

            // Domain services
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IProgressService, ProgressService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddSingleton<IQuranService, QuranService>();
            services.AddSingleton<IQuranVerseLinesService, QuranVerseLinesService>();
            services.AddScoped<IAttendanceBackgroundService, AttendanceBackgroundService>();
            services.AddScoped<IExportService, ExportService>();

            // New services
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<IHalaqaService, HalaqaService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<ITeacherAttendanceService, TeacherAttendanceService>();
            services.AddScoped<IStudentTargetService, StudentTargetService>();
            services.AddScoped<IHalaqaSupervisorService, HalaqaSupervisorService>();
            services.AddScoped<IFollowUpService, FollowUpService>();

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            return services;
        }
    }
}

