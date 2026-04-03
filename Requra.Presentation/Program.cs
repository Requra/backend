using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Requra.Infrastructure.DependencyInjection;
using Requra.Infrastructure.Validations;

namespace Requra.Presentation
{
    public class Program
    {
        public static async Task  Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            //Services Registration
            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

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
