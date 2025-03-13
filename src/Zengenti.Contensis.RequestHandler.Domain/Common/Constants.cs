namespace Zengenti.Contensis.RequestHandler.Domain.Common;

public static class Constants
{
    public static class Headers
    {
        public const string Host = "Host";
        public const string Alias = "x-alias";
        public const string ProjectApiId = "x-project-api-id";
        public const string ProjectUuid = "x-project-uuid";
        public const string TraceEnabled = "x-trace-enabled";
        public const string SurrogateControl = "surrogate-control";
        public const string SurrogateKey = "surrogate-key";
        public const string DebugSurrogateKey = "debug-surrogate-key";
        public const string Debug = "debug";
        public const string AltDebug = "x-debug";
        public const string ContentType = "Content-Type";
        public const string TransferEncoding = "transfer-encoding";
        public const string BlockConfig = "x-block-config";
        public const string RendererConfig = "x-renderer-config";
        public const string ProxyConfig = "x-proxy-config";
        public const string BlockConfigDefault = "x-block-config-default";
        public const string RendererConfigDefault = "x-renderer-config-default";
        public const string ProxyConfigDefault = "x-proxy-config-default";
        public const string NodeVersionStatus = "x-node-versionstatus";
        public const string EntryVersionStatus = "x-entry-versionstatus";
        public const string HealthCheck = "x-healthcheck";
        public const string HidePreviewToolbar = "x-hide-preview-toolbar";

        public const string ProxyForwardedFor = "x-forwarded-for";
        public const string ProxyForwardedProto = "x-forwarded-proto";
        public const string ProxyForwardedPort = "x-forwarded-port";

        public const string RequiresAlias = "x-requires-alias";
        public const string RequiresProjectApiId = "x-requires-project-api-id";
        public const string RequiresNodeId = "x-requires-node-id";
        public const string RequiresEntryId = "x-requires-entry-id";
        public const string RequiresEntryLanguage = "x-requires-entry-language";
        public const string RequiresBlockId = "x-requires-block-id";
        public const string RequiresVersionNo = "x-requires-version-no";

        public const string IsLocalRequestHandler = "x-is-local-request-handler";
        public const string ServerType = "x-site-type";
        public const string LoadBalancerVip = "x-loadbalancer-vip";
        public const string IisHostname = "x-iis-hostname";
        public const string OrigHost = "x-orig-host";

        public const string IsIisFallback = "x-is-iis-fallback";

        public static readonly string[] ConfigHeaders =
        {
            BlockConfig,
            ProxyConfig,
            RendererConfig
        };

        public static readonly string[] ConfigHeadersWithDefaults =
        {
            BlockConfigDefault,
            ProxyConfigDefault,
            RendererConfigDefault
        };

        public static readonly string[] ProxyHeaders =
        {
            ProxyForwardedFor,
            ProxyForwardedProto,
            ProxyForwardedPort
        };

        public static readonly string[] RequiresHeaders =
        {
            RequiresAlias,
            RequiresProjectApiId,
            RequiresNodeId,
            RequiresEntryId,
            RequiresEntryLanguage,
            RequiresBlockId,
            RequiresVersionNo
        };
    }

    public static class QueryStrings
    {
        public const string EntryVersionStatus = "entry-versionstatus";
    }

    public static class ContentTypes
    {
        public const string TextCss = "text/css";
        public const string TextHtml = "text/html";
        public const string ApplicationJson = "application/json";
        public const string ApplicationJavaScript = "application/javascript";
        public const string ApplicationManifestJson = "application/manifest+json";
    }

    public static class Paths
    {
        public const string StaticPathUniquePrefix = "_";

        public static readonly string[] ApiPrefixes =
        {
            "/api/"
        };

        public static readonly string[] PassThroughPrefixes =
        {
            "/REST/UI/FormsModule/TestAccessibility",
            "/REST/Contensis/content/GetFormSettings"
        };
    }

    public static class CacheKeys
    {
        public const string AnyUpdate = "any-update";
        public const string AnyEntryUpdate = "any-entry-update";
    }

    public static class Parsing
    {
        public const string Pagelet = "pagelet";
        public const string LayoutTagName = "content";
        public const string DefaultPageletReplacement = "<!--pagelet-not-resolved-->";
        public const string RendererId = "renderer";
    }

    public static class Exceptions
    {
        public const string DataKeyForOriginalMessage = "OriginalMessage";
    }
}