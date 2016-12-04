using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedCode.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GenerateUserArticles
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string sqlConnectionString = Configuration.GetConnectionString("DefaultConnection");
            var sqlProvider = new SqlProvider();
            sqlProvider.ConnectionString = sqlConnectionString;
            var docProvider =new DocumentDBProvider(Configuration.GetConnectionString("DocumentDBUri"), Configuration.GetConnectionString("DocumentDBKey"));
            // Add framework services.
            services.AddMvc();
            services.AddSingleton(typeof(DocumentDBProvider), docProvider);
            services.AddSingleton(typeof(SqlProvider), sqlProvider);

            var storageAccount = CloudStorageAccount.Parse(Configuration.GetConnectionString("StorageConnectionString"));
            // Create the table client.
            var tableClient = storageAccount.CreateCloudTableClient();
            services.AddSingleton(typeof(CloudTableClient), tableClient);

            Task.Run(() =>
            {
                var generateArticles = new Services.GenerateArticles(sqlConnectionString, docProvider, tableClient);
                generateArticles.GenerateUsersArticles();

            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }
    }
}
