using System.Reflection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Extensions;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs
{
    public static class SpecHelper
    {
        internal static readonly SiteConfigLoader SiteConfigLoaderFromFile =
            new SiteConfigLoader("Config/site_config.yaml");

        internal static readonly SiteConfigLoader SiteConfigLoaderFromJson = new SiteConfigLoader(
            "test",
            "website",
            GetFile("Config/blocks.json"),
            GetFile("Config/renderers.json"),
            "token1",
            "client1",
            "secret1",
            iisHostname: "www.mysite.com",
            podIngressIp: "10.0.0.1");

        public static SiteConfigLoader SiteConfigLoader => SiteConfigLoaderFromJson;

        public static string GetFile(string path)
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(dir!, path);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            return File.ReadAllText(filePath);
        }

        public static string NormalizeLineEndings(this string value)
        {
            return value.Replace("\r\n", "\n");
        }

        public static ILocalDevPublishingService CreatePublishingService(
            IRouteInfoFactory routeInfoFactory,
            bool enableFullUriRouting = false)
        {
            return new SiteConfigPublishingService(
                SiteConfigLoader,
                routeInfoFactory,
                Substitute.For<ICacheKeyService>(),
                enableFullUriRouting);
        }

        public static IRequestContext CreateRequestContext(bool traceEnabled = false)
        {
            return new LocalRequestContext(SiteConfigLoader, traceEnabled);
        }

        public static HtmlResponseResolver CreateHtmlResponseResolver(bool traceEnabled = false)
        {
            var requestContext = CreateRequestContext(traceEnabled);
            return new HtmlResponseResolver(
                requestContext,
                CreatePublishingService(new RouteInfoFactory(requestContext, new AppConfiguration())),
                Substitute.For<IGlobalApi>(),
                Substitute.For<ILogger<HtmlResponseResolver>>())
            {
                RequestService = Substitute.For<IEndpointRequestService>()
            };
        }

        public static void SetEndpointResponse(
            IEndpointRequestService requestService,
            string path,
            string responseContent,
            int statusCode = 200,
            PageletPerformanceData pageletPerformanceData = null)
        {
            requestService
                .Invoke(
                    HttpMethod.Get,
                    Arg.Any<Stream>(),
                    Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                    Arg.Is<RouteInfo>(r => r.Uri.AbsolutePath == path),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(
                    new EndpointResponse(
                        responseContent,
                        HttpMethod.Get,
                        new Dictionary<string, IEnumerable<string>>(),
                        statusCode,
                        pageletPerformanceData));
        }

        public static RouteInfo CreateBasicRouteInfo(
            Guid? projectUuid = null,
            Guid? blockVersionId = null,
            params string[] staticPaths)
        {
            var blockVersionInfo = new BlockVersionInfo(
                projectUuid ?? Guid.NewGuid(),
                "",
                blockVersionId ?? Guid.NewGuid(),
                new Uri("http://website.com"),
                "master",
                false,
                staticPaths,
                1);
            blockVersionInfo.EnsureDefaultStaticPaths();
            return new RouteInfo(
                RouteType.Block,
                new Uri("http://website.com"),
                new Headers(),
                "",
                blockVersionInfo);
        }
    }
}