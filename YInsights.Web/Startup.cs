using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using YInsights.Web.Providers;
using YInsights.Web.Services;
using YInsights.Web.Model;
using Microsoft.Extensions.DependencyInjection.Extensions;
using cloudscribe.Syndication.Models.Rss;

namespace YInsights
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
            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            else
            {
                builder.AddApplicationInsightsSettings();

            }
            Configuration = builder.Build();


        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add authentication services
            services.AddAuthentication(
                options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Add framework services.
            services.AddMvc();
          
            // Add functionality to inject IOptions<T>
            services.AddOptions();

            services.AddEntityFrameworkSqlServer();

          //  services.AddSingleton(typeof(RedisProvider),new RedisProvider(Configuration.GetConnectionString("RedisConnection")));
            services.AddSingleton(typeof(DocumentDBProvider), new DocumentDBProvider(Configuration.GetConnectionString("DocumentDBUri"), Configuration.GetConnectionString("DocumentDBKey")));
            services.AddTransient(typeof(UserArticleService));
            services.AddTransient(typeof(UserService));
            services.AddTransient(typeof(TopicService));
            services.AddSingleton(typeof(AIService));

            services.AddScoped<RssChannelProvider>();
            // services.AddMemoryCache();

            services.AddDbContext<YInsightsContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddApplicationInsightsTelemetry(Configuration);
            // Add the Auth0 Settings object so it can be injected
            services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Auth0Settings> auth0Settings)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            
            app.UseStaticFiles();

            // Add the cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });
            app.UseApplicationInsightsRequestTelemetry();
            // Add the OIDC middleware
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions("Auth0")
            {
                // Set the authority to your Auth0 domain
                Authority = $"https://{auth0Settings.Value.Domain}",

                // Configure the Auth0 Client ID and Client Secret
                ClientId = auth0Settings.Value.ClientId,
                ClientSecret = auth0Settings.Value.ClientSecret,

                // Do not automatically authenticate and challenge
                AutomaticAuthenticate = false,
                AutomaticChallenge = false,
                UseTokenLifetime = true,
                SaveTokens = true,
                // Set response type to code
                ResponseType = "code",
             
                // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
                CallbackPath = new PathString("/signin-auth0"),
                
                // Configure the Claims Issuer to be Auth0
                ClaimsIssuer = "Auth0"
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

          
            app.UseApplicationInsightsExceptionTelemetry();
            //app.Use(async (context, next) =>
            //{
            //    if (context.Request.IsHttps)
            //    {
            //        await next();
            //    }
            //    else
            //    {
            //        var withHttps = "https://" + context.Request.Host + context.Request.Path;
            //        context.Response.Redirect(withHttps);
            //    }
            //});
        }
    }
}
