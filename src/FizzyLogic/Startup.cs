namespace FizzyLogic
{
    using System;
    using FizzyLogic.Data;
    using FizzyLogic.Models;
    using FizzyLogic.Services;
    using FluentValidation.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Startup class for the website.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of <see cref="Startup"/>.
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Configures the dependencies for the application.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // This data context is used for the razor components.
            // We need to create an instance per operation.
            _ = services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                _ = options.UseNpgsql(_configuration.GetConnectionString("DefaultDatabase"));
            });

            // This data context is used for the razor pages. 
            // ASP.NET Core will make sure we have an instance per request.
            _ = services.AddDbContext<ApplicationDbContext>(options =>
            {
                _ = options.UseNpgsql(_configuration.GetConnectionString("DefaultDatabase"));
            });

            _ = services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            _ = services.AddAuthentication();
            _ = services.AddAuthorization();

            _ = services.AddControllers();

            _ = services.AddServerSideBlazor(options =>
                {
                    options.DetailedErrors = true;
                });

            _ = services
                .AddRazorPages()
                .AddFluentValidation(options =>
                {
                    _ = options.RegisterValidatorsFromAssembly(typeof(Startup).Assembly);
                });

            _ = services
                .AddSingleton<Slugifier>()
                .AddSingleton<IImageService, ImageService>();
        }

        /// <summary>
        /// Configures the middleware for the application.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="configuration"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            // The inital setup section helps me set up an initial user in the database.
            // Otherwise I would have to build a complete password reset/registration system.
            InitialSetup.EnsureSuperUser(configuration, serviceProvider).Wait();

            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage();
            }

            // The production environment has SSL termination in nginx.
            // On development I do want to use SSL to make sure that everything works as expected.
            if (env.IsDevelopment())
            {
                _ = app.UseHttpsRedirection();
            }

            // This middleware makes sure that everything operates as expected behind a reverse proxy.
            _ = app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // On nginx I'm using a static mapping to host the content in wwwroot. 
            // So I don't need the static files middleware. Blazor is the one evil exception why I need the static 
            // files middleware anyway.
            _ = app
                .UseStaticFiles()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    _ = endpoints.MapBlazorHub();
                    _ = endpoints.MapRazorPages();
                    _ = endpoints.MapControllers();
                });
        }
    }
}
