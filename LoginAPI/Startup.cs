using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RepositoryLayer;
using DomainLayer;
using ServiceLayer;
using DomainLayer.Models;
using MailKit;
using ServiceLayer.Interfaces;
using RepositoryLayer.Interfaces;
using RepositoryLayer.Repository;
using ServiceLayer.Interfaces.ICommonService;
using RepositoryLayer.Repository.CommonRepository;
using ServiceLayer.Services.CommonService;
using RepositoryLayer.Interfaces.ICommonRepository;
using ServiceLayer.Services.Encryption;
using ServiceLayer.Interfaces.IEncription;
using ServiceLayer.Services.Email;

namespace LoginAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Inject AppSettings
            services.Configure<ApplicationSettings>(Configuration.GetSection("ApplicationSettings"));

            services.Configure<MailSettings>(Configuration.GetSection("MailSettings"));
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IEncryption, Encryption>();
            services.AddTransient<IMessageService, ServiceLayer.MessageService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<ILoggerRepository, LoggerRepository>();
            services.AddScoped<IInMemoryCache, InMemoryCache>();
            services.AddMemoryCache();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CosmosMW", Version = "v1" });
            });

            //DB Connection
            string connection = Configuration.GetConnectionString("LoginConnection");
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection,
                                                         b => b.MigrationsAssembly("LoginAPI"))
            );

            //Identity
            services.AddDefaultIdentity<ApplicationUser>()
             .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });

            services.AddControllers().AddNewtonsoftJson();

            //CORS
            services.AddCors();

            //JWT Authentication

            var key = Encoding.UTF8.GetBytes(Configuration["Jwt:Key"].ToString());

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x => {
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseCors(builder => builder.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CosmosMW v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            //CORS
            //app.UseCors(builder =>
            //builder.WithOrigins(Configuration["Jwt:Client_Url"].ToString())
            //.AllowAnyHeader()
            //.AllowAnyMethod()
            //);



            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


        }
    }
}
