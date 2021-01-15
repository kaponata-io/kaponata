// <copyright file="Startup.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

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

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
