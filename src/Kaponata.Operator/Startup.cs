// <copyright file="Startup.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Kaponata.Kubernetes;
using Kaponata.Operator.Operators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kaponata.Operator
{
    /// <summary>
    /// This class contains startup information for the Kaponata Operator.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configures services that are used by the Kaponata Operator.
        /// </summary>
        /// <param name="services">
        /// The service collection to which to add the services.
        /// </param>
        /// <seealso href="http://go.microsoft.com/fwlink/?LinkID=398940"/>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            services.AddKubernetes();
            services.AddHostedService(serviceProvider => RedroidOperator.BuildRedroidOperator(serviceProvider).Build());
            services.AddFakeOperators();
        }

        /// <summary>
        /// Specifies how the ASP.NET application will respond to individual HTTP requests.
        /// </summary>
        /// <param name="app">
        /// The application to configure.
        /// </param>
        /// <param name="env">
        /// The current hosting environment.
        /// </param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use((context, next) =>
            {
                context.Response.Headers["X-Kaponata-Version"] = ThisAssembly.AssemblyInformationalVersion;
                return next.Invoke();
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello World!");
                });

                endpoints.MapHealthChecks("/health/ready");
                endpoints.MapHealthChecks("/health/alive");
            });
        }
    }
}
