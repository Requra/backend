using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Requra.Application.Interfaces.IAIService;
using Requra.Infrastructure.DependencyInjection;
using Requra.Infrastructure.ExternalServices.AIClient;
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

            builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateProjectValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UploadAvatarDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileDtoValidator>();


            //commented as we have a fake one now!
            //builder.Services.AddHttpClient<IAIClient, AIClient>(client =>
            //{
            //    client.BaseAddress = new Uri("https://ai-service.com");
            //    client.Timeout = TimeSpan.FromSeconds(60);
            //});
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

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
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
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
