using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using pdf_editor_api.Service;
using pdf_editor_api.Utility.Swashbuckle;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace pdf_editor_api
{
    /// <summary>
    ///     Startup.cs class
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     Startup Controller with DI
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///     Contains configuration from appsettins.json file
        /// </summary>
        public IConfiguration Configuration { get; }

        private const string _defaultCorsPolicy = "CorsPolicy";

        #region IServiceCollection Configuration
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            ServiceLogging(services);

            Log.Information($"Registering Controllers");
            services.AddControllers().AddNewtonsoftJson();

            Log.Information($"Registering Swashbuckle");
            ServiceSwashbuckle(services);

            Log.Information($"Registering CORS policy");
            ServiceCors(services);

            Log.Information($"Registering Dependency Injection");
            services.AddSingleton<PDFEditorService>();

            Log.Information($"Registering Application Insights");
            services.AddApplicationInsightsTelemetry();
        }
        #endregion

        #region IApplicationBuilder Configuration
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="provider"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCors(_defaultCorsPolicy);

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            ApplicationSwashbuckle(app, provider);
        }
        #endregion

        #region Register Swasbuckle Application Builder
        private void ApplicationSwashbuckle(IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }

                    options.RoutePrefix = string.Empty;
                });
        }
        #endregion

        #region Register Logging Service Collection
        /// <summary>
        ///     Register Logging IServiceCollection
        /// </summary>
        /// <param name="services"></param>
        private void ServiceLogging(IServiceCollection services)
        {
            var logger = new LoggerConfiguration()
                            .WriteTo.Console(LogEventLevel.Information)
                            .WriteTo.File(@"D:\home\LogFiles\AppLog.log", rollingInterval: RollingInterval.Day)
                            .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces, LogEventLevel.Information)
                            .CreateLogger();
            services.AddSingleton(logger);

            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console(LogEventLevel.Information)
                        .WriteTo.File(@"D:\home\LogFiles\AppLog.log", rollingInterval: RollingInterval.Day)
                        .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces, LogEventLevel.Information)
                        .CreateLogger();
            services.AddSingleton(Log.Logger);
        }
        #endregion

        #region Register Swashbuckle Service Collection
        /// <summary>
        ///     Register Swashbuckle IServiceCollection
        /// </summary>
        /// <param name="services"></param>
        private void ServiceSwashbuckle(IServiceCollection services)
        {
            services.AddMvc();
            services.AddApiVersioning();
            services.AddVersionedApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(x =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                x.IncludeXmlComments(xmlPath);
            });
        }
        #endregion

        #region Register CORS Service Collection
        private void ServiceCors(IServiceCollection services)
        {
            services.AddCors(x => x.AddPolicy(_defaultCorsPolicy,
                options => {
                    options.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                ;
                }));
        }
        #endregion


    }
}
