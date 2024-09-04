using System.Web;
using Zengenti.Contensis.RequestHandler.Application.Parsing;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving
{
    public class HtmlResponseResolver : IResponseResolver
    {
        private readonly IPublishingService _publishingService;
        private readonly IRequestContext _requestContext;
        private readonly IResponseResolver _genericResponseResolver;
        private readonly ILogger<HtmlResponseResolver> _logger;
        private readonly IGlobalApi _globalApi;

        public IEndpointRequestService? RequestService { get; set; }

        // Temporary way to disable pagelet resolution in production until it can be controlled from a Renderer 
        public static bool ParsePagelets { get; set; }

        public HtmlResponseResolver(
            IRequestContext requestContext,
            IPublishingService publishingService,
            IGlobalApi globalApi,
            ILogger<HtmlResponseResolver> logger)
        {
            _requestContext = requestContext;
            _publishingService = publishingService;
            _globalApi = globalApi;
            _logger = logger;
            _genericResponseResolver = new GenericResponseResolver();
        }

        public async Task<string> Resolve(string content, RouteInfo routeInfo, int currentDepth, CancellationToken ct)
        {
            content = await _genericResponseResolver.Resolve(content, routeInfo, currentDepth, ct);

            SetGeneratorMetaTag(ref content);

            if ((routeInfo.Headers.SiteType.EqualsCaseInsensitive("staging") ||
                 routeInfo.Headers.SiteType.EqualsCaseInsensitive("test")) &&
                !routeInfo.Headers.HidePreviewToolbar.EqualsCaseInsensitive("true"))
            {
                var entryVersionStatus = routeInfo.Headers.EntryVersionStatus ?? "published";
                var isContensisSingleSignOn = await _globalApi.IsContensisSingleSignOn();
                SetPreviewToolbar(
                    ref content,
                    _requestContext.Alias,
                    _requestContext.ProjectApiId,
                    entryVersionStatus,
                    routeInfo.Uri.Query,
                    isContensisSingleSignOn);
            }

            return ParsePagelets
                ? await ResolveHtml(content, routeInfo, currentDepth, ct)
                : content;
        }

        public static void SetPreviewToolbar(
            ref string content,
            string alias,
            string projectApiId,
            string entryVersionStatus,
            string query,
            bool isContensisSingleSignOn)
        {
            var queryDictionary = HttpUtility.ParseQueryString(query);
            var entryId = queryDictionary["entryId"];
            //Todo Get Language from routeInfo and ideally the EntryID
            var entryLanguageId = "en-GB";
            DateTime roundedDate = new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day,
                DateTime.Now.Hour,
                0,
                0);

            var isContensisSsoScriptValue = isContensisSingleSignOn.ToString().ToLowerInvariant();
            var scriptTag =
                $"<script type=\"text/javascript\">window.ContensisProjectApiId=\"{projectApiId}\";window.ContensisAlias=\"{alias}\";window.ContensisSso=\"{isContensisSsoScriptValue}\";window.ContensisEntryVersionStatus=\"{entryVersionStatus}\";window.ContensisEntryId=\"{entryId}\";window.ContensisEntryLanguage=\"{entryLanguageId}\"</script>";
            scriptTag +=
                $"<script type=\"module\" src=\"/contensis-preview-toolbar/esm/index.js?d={roundedDate.Ticks.ToString()}\"></script></body>";
            content = content.Replace("</body>", scriptTag);
        }

        private void SetGeneratorMetaTag(ref string content)
        {
            var metaTag = "<meta name=\"generator\" content=\"Contensis\" /></head>";
            content = content.Replace("</head>", metaTag);
        }

        private async Task<string> ResolveHtml(
            string content,
            RouteInfo routeInfo,
            int currentDepth,
            CancellationToken ct)
        {
            var parser = new HtmlParser(content);
            var resolvedContent = new Lazy<HtmlContent>(() => new HtmlContent(content, _logger));
            var resolvingTasks = new List<Task>();

            while (parser.ParseNext(
                new[]
                {
                    Constants.Parsing.Pagelet
                },
                out var tag))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                resolvedContent.Value.AddTagOffset(tag);
                resolvingTasks.Add(ResolvePagelet(resolvedContent.Value, tag, routeInfo, currentDepth + 1, ct));
            }

            if (routeInfo.LayoutRendererId.HasValue)
            {
                resolvingTasks.Add(
                    ResolveLayout(
                        resolvedContent.Value,
                        routeInfo.LayoutRendererId.Value,
                        currentDepth + 1,
                        ct));
            }

            var allTasks = Task.WhenAll(resolvingTasks);
            try
            {
                await allTasks;
            }
            catch
            {
                if (allTasks != null && allTasks.IsFaulted)
                {
                    throw allTasks.Exception!;
                }

                throw;
            }

            if (resolvedContent.IsValueCreated)
            {
                return resolvedContent.ToString()!;
            }

            return content;
        }

        private async Task ResolvePagelet(
            HtmlContent content,
            HtmlTag tag,
            RouteInfo routeInfo,
            int currentDepth,
            CancellationToken ct)
        {
            var replacement = Constants.Parsing.DefaultPageletReplacement;
            var rendererId = tag.Attributes
                .FirstOrDefault(a => a.Key.EqualsCaseInsensitive(Constants.Parsing.RendererId))
                .Value;

            if (!string.IsNullOrWhiteSpace(rendererId))
            {
                var endpointResponse = await InvokeEndpoint(rendererId, routeInfo.Headers, currentDepth, ct);
                if (_requestContext.TraceEnabled)
                {
                    replacement = endpointResponse.PageletPerformanceData + endpointResponse.StringContent;
                }
                else
                {
                    replacement = endpointResponse.StringContent;
                }
            }

            await content.UpdateTag(tag.Id, replacement!);
        }

        private async Task<EndpointResponse> InvokeEndpoint(
            string rendererId,
            Headers headers,
            int currentDepth,
            CancellationToken ct)
        {
            var messageSuffix = $" for alias {_requestContext.Alias} and project {_requestContext.ProjectApiId}.";
            try
            {
                var routeInfo =
                    await _publishingService.GetRouteInfoForRequest(
                        _requestContext.ProjectUuid,
                        headers,
                        rendererId,
                        null);
                var response =
                    await RequestService!.Invoke(HttpMethod.Get, null, headers, routeInfo!, currentDepth, ct);

                if (!response.IsSuccessStatusCode())
                {
                    throw new EndpointException(
                        routeInfo,
                        response,
                        $"Received error status code {response.StatusCode} when invoking endpoint {routeInfo!.EndpointId}{messageSuffix}");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to invoke endpoint for renderer {RendererId}{MessageSuffix}",
                    rendererId,
                    messageSuffix);
                throw;
            }
        }

        private async Task ResolveLayout(
            HtmlContent content,
            Guid layoutRendererId,
            int currentDepth,
            CancellationToken ct)
        {
            var layoutRouteInfo = await _publishingService.GetRouteInfoForRequest(
                _requestContext.ProjectUuid,
                false,
                null,
                new Headers(),
                null,
                null,
                layoutRendererId.ToString());

            if (layoutRouteInfo == null)
            {
                // TODO: Decide what happens
                return;
            }

            var layoutContentResponse =
                await RequestService!.Invoke(HttpMethod.Get, null, null, layoutRouteInfo, currentDepth, ct);

            if (!layoutContentResponse.IsSuccessStatusCode())
            {
                throw new EndpointException(
                    layoutRouteInfo,
                    layoutContentResponse,
                    $"Received error status code {layoutContentResponse.StatusCode} when invoking endpoint {layoutRouteInfo.EndpointId}.");
            }

            HtmlTag? contentTag = null;
            var parser = new HtmlParser(layoutContentResponse.StringContent!);

            while (parser.ParseNext(
                new[]
                {
                    Constants.Parsing.LayoutTagName
                },
                out var htmlTag))
            {
                contentTag = htmlTag;
                break;
            }

            if (contentTag != null)
            {
                await content.WrapWithLayout(
                    layoutContentResponse.StringContent!,
                    contentTag.StartPos,
                    contentTag.EndPos);
            }
        }
    }
}