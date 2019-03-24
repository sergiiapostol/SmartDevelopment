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
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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


        private void AddConfiguration(IServiceCollection services)
        {
            services.Configure<JwtTokenConfiguration>("JwtToken", Configuration);

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

            services.AddOptions<ConnectionSettings>()
                .Configure(options =>
                    options.ConnectionString = Configuration.GetConnectionString("MongoDb"));

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

            services.Configure<DependencySettings>("DependencySettings", Configuration);
            services.Configure<LoggerSettings>("LoggerSettings", Configuration);
            services.Configure<ProfilingSettings>("MongoDbProfilingSettings", Configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

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

            //services.AddLogger();

            services.AddDependencyTrackingWithApplicationInsights();

            services.AddMongoDb();
            services.AddProfiledMongoDb();

            services.AddMongoDbIdentity()
                .AddIdentity<Identity.Entities.IdentityUser, Identity.Entities.IdentityRole>()
                .AddMongodbStores()
                .AddDefaultTokenProviders();

            services.AddSingleton<JwtSecurityTokenHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseResponseCaching();

            app.UseAuthentication();

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartGrocery API V1");
                c.EnableValidator();
                c.DisplayRequestDuration();
                c.EnableFilter();
            });
        }
    }
}
