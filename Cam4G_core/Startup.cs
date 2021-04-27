
using Cam4G_core.App_Data;
using Cam4G_core.Services;
using Core;
using Core.Services;
using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using Services;
using System.Text;

namespace Cam4G_core
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private Logger _logger;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            //單體模式建立AppConfig物件
            Config.Initial(Configuration);
            _logger = LogManager.GetCurrentClassLogger();

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            services.AddScoped<IUnitOfWork, UnitOfWork>(o => 
                new UnitOfWork(Config.GetSetting("ConnectionStrings:ApplicationServices"), this._logger)
            );
            services.AddTransient<IIOService, IOService>(o => new IOService(this._logger));
            
            services.AddTransient<IGCMService, GCMService>();
            services.AddTransient<IS3Service, S3Service>(o => new S3Service
            { 
                _logger = this._logger,
                _AWSAccessKeyId = Config.GetSetting("S3:AWSAccessKeyId"),
                _AWSSecretKey = Config.GetSetting("S3:AWSSecretKey"),
                _bucketRegion = Config.GetSetting("S3:BucketRegion"),
                _BucketName = Config.GetSetting("S3:BucketName")
            });
            services.AddTransient<IPushNotificationService, PushNotificationService>(o => new PushNotificationService(this._logger));
            services.AddTransient<IXmlService, XmlService>();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
                    await context.Response.WriteAsync("Hello World");
                });

                endpoints.MapControllers();
                //endpoints.MapControllerRoute(
                //    name: "DefaultApi",
                //    pattern: "api/{controller}/{action?}"
                //);
            });
        }
    }
}
