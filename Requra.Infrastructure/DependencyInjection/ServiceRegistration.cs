using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Interfaces.IProfileService;
using Requra.Application.Interfaces.IProjectRepository;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService;
using Requra.Application.Interfaces.IRecordingService;
using Requra.Application.Mappings;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.ICloudinaryService;
using Requra.Infrastructure.ExternalInterfaces.IExternalAuth;
using Requra.Infrastructure.ExternalInterfaces.IJwtTokenService;
using Requra.Infrastructure.ExternalServices.CloudinaryService;
using Requra.Infrastructure.ExternalServices.ExternalAuth;
using Requra.Infrastructure.Initializers;
using Requra.Infrastructure.Repositories.Project;
using Requra.Infrastructure.Services.AuthService;
using Requra.Infrastructure.Services.DocumentService;
using Requra.Infrastructure.Services.JWTService;
using Requra.Infrastructure.Services.ProfileService;
using Requra.Infrastructure.Services.ProjectService;
using Requra.Infrastructure.Services.ProjectService.ProjectResultsService.UserStoryService;
using Requra.Infrastructure.Services.RecordingService;
using Requra.Infrastructure.UnitOfWork;
using System.Text;

namespace Requra.Infrastructure.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {

            // Add Postgresql Setting
            services.AddDbContext<RequraDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("NeonConnection"))
            );



            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<RequraDbContext>()
                    .AddDefaultTokenProviders();



            //External Services Registration
            services.AddScoped<IJwtTokenService, JwtTokenService>();
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
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(configuration["Jwt:Key"])
                )
                        };
                    });

            //external services registration
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();


            //Application Services Registration
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IUserStoryService, UserStoryService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IRecordingService, RecordingService>();
            services.AddScoped<IRecordingBackgroundJobService, RecordingBackgroundJobService>();
            services.AddScoped<IRecordingFinalizationService, RecordingFinalizationService>();
            services.AddScoped<IRecordingChunkStorageReader, RecordingChunkStorageReader>();



            // Auto Mapper
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());


            return services;
        }
        public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
        {
            await DatabaseInitializer.InitializeAsync(app.ApplicationServices);
        }
    }

}
