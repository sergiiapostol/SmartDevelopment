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
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using SmartDevelopment.ApplicationInsight.Extensions;
using SmartDevelopment.Caching.OutputCaching;
using SmartDevelopment.AzureStorage;
using SmartDevelopment.AzureStorage.Blobs;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.DependencyTracking;
using SmartDevelopment.DependencyTracking.ApplicationInsights;
using SmartDevelopment.DependencyTracking.MongoDb;
using SmartDevelopment.Identity;
using SmartDevelopment.Logging;
using SmartDevelopment.SampleApp.AspCore.Configuration;
using SmartDevelopment.Messaging;
using SmartDevelopment.SampleApp.AspCore.Controllers;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Caching.EnrichedMemoryCache.Distributed;
using SmartDevelopment.Dal.Cached;

namespace SmartDevelopment.SampleApp.AspCore
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            Environment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        private IWebHostEnvironment Environment { get; }

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
            services.Configure<ResponseCachingSettings>(Configuration.GetSection("ResponseCachingSettings"));
            services.Configure<EnrichedMemoryCacheSettings>(Configuration.GetSection("EnrichedMemoryCacheSettings"));
            services.Configure<DistributedEnrichedMemoryCacheSettings>(Configuration.GetSection("DistributedEnrichedMemoryCacheSettings"));
            services.Configure<DependencyFilterSettings>(Configuration.GetSection("DependencyByNameFilterSettings"));
            services.Configure<RequestsByNameFilterSettings>(Configuration.GetSection("RequestsByNameFilterSettings"));
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
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<RequestsBySynteticSourceFilter>();
            services.AddApplicationInsightsTelemetryProcessor<RequestsByNameFilter>();
            services.AddApplicationInsightsTelemetryProcessor<DependencyByNameFilter>();

            AddConfiguration(services);

            services.AddHttpContextAccessor();
            services.AddResponseCompression();
            services.AddResponseCaching();

            services.AddMemoryCache(v => { v.CompactionPercentage = 0.9; });
            services.AddDistributedEnrichedMemoryCacheInitializer();

            services.AddSingleton<OutputCacheManager>();            

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            // Add framework services.
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.CacheProfiles.Add("Default",
                    new CacheProfile
                    {
                        Duration = 3600,
                        Location = ResponseCacheLocation.Any
                    });
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            }).AddControllersAsServices();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartDevelopment.SampleApp.AspCore API", Version = "v1" });
                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please insert JWT with Bearer into field",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                },
                                Scheme = "oauth2",
                                Name = "Bearer",
                                In = ParameterLocation.Header,

                            },
                            new List<string>()
                        }
                    });
            });
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
            services.AddBlobQueuesInitializer(new AzureStorage.ConnectionSettings { ConnectionString = Configuration.GetConnectionString("AzureStorage") });
            services.AddServiceBusQueuesInitializer(new ServiceBus.ConnectionSettings { ConnectionString = Configuration.GetConnectionString("ServiceBus") });

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.Register(_ => Configuration).As<IConfiguration>().SingleInstance();
            builder.RegisterType<TestQueueSender>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestQeueuReceiver>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestTopicSender>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestTopicReceiver>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestDal>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestDalCached>().As<IDalCached<TestEntity>>().SingleInstance();
            var autofaccontainer = builder.Build();

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(autofaccontainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMiddleware<CachingMiddleware>();

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
                await indexManager.UpdateIndexes();

                var blobs = app.ApplicationServices.GetService<BlobsInitializator>();
                await blobs.Init();

                var queues = app.ApplicationServices.GetService<ChannelsInitializator>();                
                await queues.Init();
            });
        }
    }
}