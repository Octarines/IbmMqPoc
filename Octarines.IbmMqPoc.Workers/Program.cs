using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Octarines.IbmMqPoc.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Octarines.IbmMqPoc.Workers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => ConfigureServices(hostContext.Configuration, services));

        public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.Configure<MqSettings>(configuration.GetSection(nameof(MqSettings)));

            services.AddHostedService<MessageWorker>();
        }
    }
}
