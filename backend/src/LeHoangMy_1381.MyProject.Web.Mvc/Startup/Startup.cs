using Abp.AspNetCore;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Castle.Logging.Log4Net;
using LeHoangMy_1381.MyProject.Authentication.JwtBearer;
using LeHoangMy_1381.MyProject.Configuration;
using LeHoangMy_1381.MyProject.Identity;
using LeHoangMy_1381.MyProject.Web.Resources;
using Castle.Facilities.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System;
using LeHoangMy_1381.MyProject.Web.Reading;

namespace LeHoangMy_1381.MyProject.Web.Startup;

public class Startup
{
    private const string FlutterCorsPolicy = "FlutterClient";
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IConfigurationRoot _appConfiguration;

    public Startup(IWebHostEnvironment env)
    {
        _hostingEnvironment = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // MVC
        services.AddControllersWithViews(
                options =>
                {
                    options.Filters.Add<ReadingOpenAccessFilter>(int.MinValue);
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    options.Filters.Add(new AbpAutoValidateAntiforgeryTokenAttribute());
                }
            );

        IdentityRegistrar.Register(services);
        AuthConfigurer.Configure(services, _appConfiguration);

        services.Configure<WebEncoderOptions>(options =>
        {
            options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
        });

        services.AddScoped<IWebResourceManager, WebResourceManager>();

        services.AddSignalR();

        services.AddCors(options => options.AddPolicy(FlutterCorsPolicy, policy =>
            policy.WithOrigins(_appConfiguration.GetSection("App:CorsOrigins").Get<string[]>() ?? Array.Empty<string>())
                .AllowAnyHeader()
                .AllowAnyMethod()));

        // Configure Abp and Dependency Injection
        services.AddAbpWithoutCreatingServiceProvider<MyProjectWebMvcModule>(
            // Configure Log4Net logging
            options => options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                f => f.UseAbpLog4Net().WithConfig(
                    _hostingEnvironment.IsDevelopment()
                        ? "log4net.config"
                        : "log4net.Production.config"
                    )
            )
        );
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseAbp(); // Initializes ABP framework.

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors(FlutterCorsPolicy);

        app.UseAuthentication();

        app.UseJwtTokenMiddleware();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<AbpCommonHub>("/signalr");
            endpoints.MapControllerRoute("default", "{controller=Reading}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Reading}/{action=Index}/{id?}");
        });
    }
}
