using DX.Web.Options;
using DX.Web.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSwag.Generation.AspNetCore;
using System;

namespace DX.Web
{
    public class DXWebModule : DXModule {
        public DXWebModule(IConfiguration configuration) : base(configuration) { }

        protected internal override void ConfigureServices(IServiceCollection services, IServiceProvider globalServices) {
            base.ConfigureServices(services, globalServices);

            services.AddSingleton<IPostConfigureOptions<MvcNewtonsoftJsonOptions>, MvcJsonOptionsSetup>();
            services.AddMvc(o => o.EnableEndpointRouting = false)
                .AddApplicationPart(GetType().Assembly)
                .AddNewtonsoftJson();
            services.AddHttpContextAccessor();

            services.AddSingleton<IConfigureOptions<AspNetCoreOpenApiDocumentGeneratorSettings>, OpenApiDocumentGeneratorSettingsSetup>();
            services.AddSwaggerDocument((settings, sp) => {
                sp.GetRequiredService<IOptionsInitializer<AspNetCoreOpenApiDocumentGeneratorSettings>>().Initialize(
                    settings, 
                    Microsoft.Extensions.Options.Options.DefaultName
                );
            });
        }

        protected internal override void Configure(IApplicationBuilder app) {
            base.Configure(app);
            app.UseOpenApi();
            app.UseSwaggerUi3();
            app.UseMvc();
        }
    }
}
