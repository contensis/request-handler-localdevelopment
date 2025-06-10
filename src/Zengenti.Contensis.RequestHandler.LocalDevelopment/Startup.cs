using Zengenti.Contensis.RequestHandler.Application.Middleware;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

#pragma warning disable CS9113 // Parameter is unread.
public class Startup(IConfiguration configuration)
#pragma warning restore CS9113 // Parameter is unread.
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddLazyCache();

        services.AddHttpClient("no-auto-redirect")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        services
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddSingleton<ICacheKeyService, NullCacheKeyService>()
            .AddSingleton(
                new AppConfiguration(
                    ProgramOptions.Current.PodClusterId,
                    ProgramOptions.Current.BlockClusterIngressIp,
                    ProgramOptions.Current.BlockAddressSuffix))
            .AddSingleton<IRouteInfoFactory, RouteInfoFactory>();

        services.AddSingleton<ICorePublishingService, CorePublishingService>();

        // Local HTTP/config development mode...
        SiteConfigLoader siteConfigLoader;
        if (ProgramOptions.Current.ConfigFile != null)
        {
            siteConfigLoader = new SiteConfigLoader(ProgramOptions.Current.ConfigFile);
        }
        else
        {
            var podIngressIpDictionary = new Dictionary<string, string>
            {
                {
                    "local", "127.0.0.1"
                },
                {
                    "hq", "185.18.139.20"
                },
                {
                    "hq2", "185.18.139.242"
                },
                {
                    "lon", "185.18.139.108"
                },
                {
                    "man", "185.18.139.88"
                },
            };

            siteConfigLoader = new SiteConfigLoader(
                ProgramOptions.Current.Alias!,
                ProgramOptions.Current.ProjectApiId!,
                ProgramOptions.Current.BlocksAsJson!,
                ProgramOptions.Current.RenderersAsJson,
                ProgramOptions.Current.AccessToken,
                ProgramOptions.Current.ClientId,
                ProgramOptions.Current.ClientSecret,
                ProgramOptions.Current.Username,
                ProgramOptions.Current.Password,
                ProgramOptions.Current.IisHostname,
                podIngressIpDictionary[ProgramOptions.Current.PodClusterId!]);
        }

        services.AddSingleton<ISiteConfigLoader>(_ => siteConfigLoader);
        services.AddTransient<IRequestContext, LocalRequestContext>();
        services.AddSingleton<IPublishingServiceCache, NullPublishingServiceCache>();
        services.AddSingleton<ISecurityTokenProviderFactory, SecurityTokenProviderFactory>();
        services.AddSingleton<IPublishingApi, HttpPublishingApi>();
        services.AddSingleton<IGlobalApi, HttpGlobalApi>();
        services.AddSingleton<ICorePublishingService, CorePublishingService>();
        services.AddSingleton<IPublishingService, LocalDevPublishingService>();
        services.AddSingleton<INodeService, LocalNodeService>();

        // Standard services
        services
            .AddSingleton<GenericResponseResolver>()
            .AddTransient<HtmlResponseResolver>()
            .AddSingleton<IEndpointRequestService, EndpointRequestService>()
            .AddSingleton<IRouteService, RouteService>()
            .AddSingleton<IResponseResolverService, ResponseResolverService>()
            .AddSingleton<ResponseResolverFactory>()
            .AddSingleton<IServerTypeResolver, ServerTypeResolver>()
            .AddSingleton<CallContextService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseMiddleware<RequestHandlerMiddleware>();
    }
}