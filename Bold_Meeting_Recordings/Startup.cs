using System.Net.Http;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Bold_Meeting_Recordings.Helpers;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

namespace Bold_Meeting_Recordings
{
    public class Startup
    {
        public static string Progress = "";

        public static string signOutUrl = "";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            signOutUrl = Configuration.GetSection("AzureAd")["Instance"] + Configuration.GetSection("AzureAd")["TenantId"] + "/oauth2/v2.0/logout?post_logout_redirect_uri=";
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {

                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;

            });
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                 .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
            options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
            {
                    var httpClient = new HttpClient();
                    await httpClient.GetAsync(signOutUrl + context.Request.GetEncodedUrl());

                    foreach (var cookie in context.Request.Cookies.Keys)
                    {
                        context.Response.Cookies.Delete(cookie);
                    }
                };
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.RequireHeaderSymmetry = false;
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // TODO : it's a bit unsafe to allow all Networks and Proxies...
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

           // services.AddHttpsRedirection(options =>
           //{
           //     options.HttpsPort = 443;
           // });

            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.Configure<AppSettings>(Configuration.GetSection("AwsSettings"));
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health-check");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=UploadFile}/{id?}");
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto
            });
        }
    }
}
