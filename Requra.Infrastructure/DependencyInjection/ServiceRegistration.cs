using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Interfaces.IJwtTokenService;
using Requra.Application.Mappings;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Initializers;
using Requra.Infrastructure.Services.AuthService;
using Requra.Infrastructure.Services.JWTService;
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
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            );



            // Auto Mapper
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

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



            //Application Services Registration
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();




            return services;
        }
        public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
        {
            await DatabaseInitializer.InitializeAsync(app.ApplicationServices);
        }
    }

}
