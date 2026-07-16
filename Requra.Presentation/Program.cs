using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

using Requra.Application.ApplicationServiceRegistration;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IFileDownloader;
using Requra.Infrastructure.DependencyInjection;
using Requra.Infrastructure.ExternalServices.AIClient;
using Requra.Infrastructure.Http.FileDownloader;
using Requra.Infrastructure.Validations;
using System.Text.Json.Serialization;

namespace Requra.Presentation
{
    public class Program
    {
        public static async Task  Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                });

          


            //Services Registration
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationServices();


            //should be added after services registration
            builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateProjectValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UploadAvatarDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileDtoValidator>();



            //builder.Services.AddOpenApi();
            //builder.Services.AddEndpointsApiExplorer();

            //builder.Services.AddSwaggerGen(options =>
            //{
            //    options.SwaggerDoc("v1", new OpenApiInfo
            //    {
            //        Title = "Requra API",
            //        Version = "v1"
            //    });

            //    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //    {
            //        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
            //        Name = "Authorization",
            //        In = ParameterLocation.Header,
            //        Type = SecuritySchemeType.Http,
            //        Scheme = "bearer",
            //        BearerFormat = "JWT"
            //    });

            //    options.AddSecurityRequirement(document=>new OpenApiSecurityRequirement
            //    {
            //        [new OpenApiSecuritySchemeReference("bearer", document)] = []
            //    });
            //});

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Requra API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter JWT token.\n\nExample:\nBearer eyJhbGciOiJIUzI1NiIs...",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                   [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            //CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });
            var app = builder.Build();
            await app.InitializeDatabaseAsync();
            // Configure the HTTP request pipeline.
           // if (app.Environment.IsDevelopment())
           // {
                app.MapOpenApi();
                app.UseSwagger();

                app.UseSwaggerUI(options =>
                {
                    options.DocumentTitle = "Requra API";
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Requra API v1");
                });
           // }
            
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
