using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Requra.Application.Interfaces.IAuthService;
using Requra.Application.Mappings;
using Requra.Domain.Entities;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Services.AuthService;
using Requra.Infrastructure.UnitOfWork;

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
            services.AddAutoMapper(cfg =>cfg.AddProfile<MappingProfile>());

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<RequraDbContext>()
                    .AddDefaultTokenProviders();



            //External Services Registration


            //Application Services Registration
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();
           


            return services;
        }
    }

}
