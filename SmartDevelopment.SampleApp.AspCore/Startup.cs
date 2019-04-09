using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SmartDevelopment.AzureStorage;
using SmartDevelopment.AzureStorage.Blobs;
using SmartDevelopment.AzureStorage.Queues;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.DependencyTracking;
using SmartDevelopment.DependencyTracking.ApplicationInsights;
using SmartDevelopment.DependencyTracking.MongoDb;
using SmartDevelopment.Identity;
using SmartDevelopment.Logging;
using SmartDevelopment.SampleApp.AspCore.Configuration;
using Swashbuckle.AspNetCore.Swagger;

namespace SmartDevelopment.SampleApp.AspCore
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        private IHostingEnvironment Environment { get; }

        private IConfigurationRoot Configuration { get; }

        private Logging.ILogger<Startup> ApplicationLogger { get; set; }

        private void AddConfiguration(IServiceCollection services)
        {
            var jwtTokenOptions = Configuration.GetSection("JwtToken").Get<JwtTokenConfiguration>();
            services.AddOptions<JwtBearerOptions>()
                .Configure(options =>
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = jwtTokenOptions.SecurityKey,

                        ValidateAudience = true,
                        ValidAudience = jwtTokenOptions.Audience,

                        ValidateIssuer = true,
                        ValidIssuer = jwtTokenOptions.Issuer,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    });

            services.Configure<JwtTokenConfiguration>(Configuration.GetSection("JwtToken"));

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(1);
                options.Lockout.MaxFailedAccessAttempts = 10;

                // User settings
                options.User.RequireUniqueEmail = false;

                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddApplicationInsights());

            AddConfiguration(services);

            services.AddHttpContextAccessor();
            services.AddResponseCompression();
            services.AddResponseCaching();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            // Add framework services.
            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Default",
                    new CacheProfile
                    {
                        Duration = 3600,
                        Location = ResponseCacheLocation.Any
                    });
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            }).AddControllersAsServices();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "SmartDevelopment.SampleApp.AspCore API", Version = "v1" });
                c.DescribeAllEnumsAsStrings();
                c.DescribeStringEnumsInCamelCase();
                c.AddSecurityDefinition("Bearer",
                    new ApiKeyScheme
                    {
                        In = "header",
                        Description = "Please insert JWT with Bearer into field",
                        Name = "Authorization",
                        Type = "bearer"
                    });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> { { "Bearer", new string[] { } } });
            });

            services.AddResponseCaching();

            services.AddLogger(Configuration.GetSection("LoggerSettings").Get<LoggerSettings>());

            services.AddDependencyTrackingWithApplicationInsights(Configuration.GetSection("DependencySettings").Get<DependencySettings>());

            services.AddProfiledMongoDb(
                new Dal.MongoDb.ConnectionSettings { ConnectionString = Configuration.GetConnectionString("MongoDb") }, 
                Configuration.GetSection("MongoDbProfilingSettings").Get<ProfilingSettings>());

            services
                .AddIdentity<Identity.Entities.IdentityUser, Identity.Entities.IdentityRole>()
                .AddMongoDBStores<Identity.Entities.IdentityUser, Identity.Entities.IdentityRole>()
                .AddDefaultTokenProviders();

            services.AddSingleton<JwtSecurityTokenHandler>();

            services.AddBlobsInitializer(new AzureStorage.ConnectionSettings { ConnectionString = Configuration.GetConnectionString("AzureStorage") });
            services.AddQueuesInitializer(new AzureStorage.ConnectionSettings { ConnectionString = Configuration.GetConnectionString("AzureStorage") });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "api/{controller}/{action}/{id?}");
            }).UseResponseCaching().UseResponseCompression();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartGrocery API V1");
                c.EnableValidator();
                c.DisplayRequestDuration();
                c.EnableFilter();
            });

            ApplicationLogger = app.ApplicationServices.GetService<Logging.ILogger<Startup>>();

            appLifetime.ApplicationStopped.Register(() =>
            {
                ApplicationLogger.Debug("Application ending");
            });

            appLifetime.ApplicationStarted.Register(async () =>
            {
                ApplicationLogger.Debug("Application starting");

                var indexManager = app.ApplicationServices.GetService<IndexesManager>();
                await indexManager.UpdateIndexes().ConfigureAwait(false);

                var blobs = app.ApplicationServices.GetService<BlobsInitializator>();
                await blobs.Init().ConfigureAwait(false);

                var queues = app.ApplicationServices.GetService<QueuesInitializator>();
                await queues.Init().ConfigureAwait(false);
            });
        }
    }
}