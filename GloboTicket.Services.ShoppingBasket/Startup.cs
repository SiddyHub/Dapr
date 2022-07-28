using AutoMapper;
using Dapr.Client;
using GloboTicket.Services.ShoppingBasket.DbContexts;
using GloboTicket.Services.ShoppingBasket.Repositories;
using GloboTicket.Services.ShoppingBasket.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Text.Json;

namespace GloboTicket.Services.ShoppingBasket
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddDapr(builder =>
                builder.UseJsonSerializationOptions(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                    }));

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //services.AddHostedService<ServiceBusListener>();

            var optionsBuilder = new DbContextOptionsBuilder<ShoppingBasketDbContext>();
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton(new BasketLinesIntegrationRepository(optionsBuilder.Options));

            services.AddScoped<IBasketRepository, BasketRepository>();
            services.AddScoped<IBasketLinesRepository, BasketLinesRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBasketChangeEventRepository, BasketChangeEventRepository>();

            //services.AddSingleton<IMessageBus, AzServiceBusMessageBus>();

            services.AddSingleton<IEventCatalogService>(c =>
                new EventCatalogService(DaprClient.CreateInvokeHttpClient("catalog")));
            services.AddSingleton<IDiscountService>(c =>
                new DiscountService(DaprClient.CreateInvokeHttpClient("discountgrpc")));

            services.AddDbContext<ShoppingBasketDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shopping Basket API", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopping Basket API V1");

            });

            app.UseRouting();

            app.UseCloudEvents();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
            });
        }        
    }
}
