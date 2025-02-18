﻿namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public record AppConfiguration(
    string? BlockClusterIngressIp = null,
    string? BlockAddressSuffix = null,
    string[]? AliasesWithApiRoutes = null,
    string? ServicePod = null,
    string? DataCenter = null,
    string? NodesDeliveryHost = null);