using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StronglyTypedApiClient.Generator;

[Generator]
public sealed class StronglyTypedApiClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<AdditionalText> contractFiles = context.AdditionalTextsProvider
            .Where(static file => Path.GetFileName(file.Path).Equals("api-contract.json", StringComparison.OrdinalIgnoreCase));

        IncrementalValueProvider<ImmutableArray<(string Path, string? Content)>> contracts = contractFiles
            .Select(static (file, cancellationToken) => (file.Path, file.GetText(cancellationToken)?.ToString()))
            .Collect();

        context.RegisterSourceOutput(contracts, static (productionContext, items) =>
        {
            if (items.IsDefaultOrEmpty)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingContractFile, Location.None));
                return;
            }

            (string _, string? content) = items[0];
            if (string.IsNullOrWhiteSpace(content))
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidContractJson, Location.None, "api-contract.json is empty."));
                return;
            }

            if (!TryParseContract(content, out ContractModel? contract, out IReadOnlyList<Diagnostic> parseDiagnostics))
            {
                foreach (Diagnostic diagnostic in parseDiagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);
                }

                return;
            }

            string source = GenerateSource(contract!);
            productionContext.AddSource("TypedApiClient.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static bool TryParseContract(
        string json,
        out ContractModel? contract,
        out IReadOnlyList<Diagnostic> diagnostics)
    {
        List<Diagnostic> results = new();
        contract = null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            string clientName = GetRequiredString(root, "clientName", Diagnostics.MissingClientName, results);
            string targetNamespace = GetRequiredString(root, "namespace", Diagnostics.MissingNamespace, results);

            List<EndpointModel> endpoints = new();
            if (!root.TryGetProperty("endpoints", out JsonElement endpointsElement) || endpointsElement.ValueKind != JsonValueKind.Array)
            {
                results.Add(Diagnostic.Create(Diagnostics.MissingEndpoints, Location.None));
            }
            else
            {
                foreach (JsonElement endpointElement in endpointsElement.EnumerateArray())
                {
                    string endpointName = GetRequiredString(endpointElement, "name", Diagnostics.MissingEndpointName, results);
                    string method = GetRequiredString(endpointElement, "method", Diagnostics.MissingHttpMethod, results);
                    string route = GetRequiredString(endpointElement, "route", Diagnostics.MissingRoute, results);

                    if (!string.IsNullOrWhiteSpace(method) && !IsAllowedMethod(method))
                    {
                        results.Add(Diagnostic.Create(Diagnostics.InvalidHttpMethod, Location.None, method));
                    }

                    EndpointPayloadModel? request = ParsePayload(endpointElement, "request", Diagnostics.MissingRequest, results);
                    EndpointPayloadModel? response = ParsePayload(endpointElement, "response", Diagnostics.MissingResponse, results);

                    endpoints.Add(new EndpointModel(endpointName, method, route, request, response));
                }
            }

            if (results.Count > 0)
            {
                diagnostics = results;
                return false;
            }

            contract = new ContractModel(clientName, targetNamespace, endpoints);
            diagnostics = results;
            return true;
        }
        catch (JsonException ex)
        {
            diagnostics = new[] { Diagnostic.Create(Diagnostics.InvalidContractJson, Location.None, ex.Message) };
            return false;
        }
    }

    private static EndpointPayloadModel? ParsePayload(
        JsonElement endpointElement,
        string propertyName,
        DiagnosticDescriptor missingDescriptor,
        List<Diagnostic> diagnostics)
    {
        if (!endpointElement.TryGetProperty(propertyName, out JsonElement payloadElement) || payloadElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(Diagnostic.Create(missingDescriptor, Location.None));
            return null;
        }

        string payloadName = GetRequiredString(payloadElement, "name", missingDescriptor, diagnostics);

        Dictionary<string, string> properties = new(StringComparer.Ordinal);
        if (!payloadElement.TryGetProperty("properties", out JsonElement propertiesElement) || propertiesElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(Diagnostic.Create(missingDescriptor, Location.None));
            return null;
        }

        foreach (JsonProperty property in propertiesElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidPropertyType, Location.None, property.Name));
                continue;
            }

            string? typeName = property.Value.GetString();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidPropertyType, Location.None, property.Name));
                continue;
            }

            properties[ToPascalCase(property.Name)] = typeName;
        }

        return new EndpointPayloadModel(payloadName, properties);
    }

    private static string GenerateSource(ContractModel contract)
    {
        StringBuilder builder = new();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Net.Http;");
        builder.AppendLine("using System.Net.Http.Json;");
        builder.AppendLine("using System.Text;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.Append("namespace ").Append(contract.TargetNamespace).AppendLine(";");
        builder.AppendLine();

        foreach (EndpointModel endpoint in contract.Endpoints.OrderBy(static endpoint => endpoint.Name, StringComparer.Ordinal))
        {
            EmitPayload(builder, endpoint.Request!);
            EmitPayload(builder, endpoint.Response!);
        }

        string interfaceName = $"I{contract.ClientName}";
        builder.Append("public interface ").Append(interfaceName).AppendLine();
        builder.AppendLine("{");

        foreach (EndpointModel endpoint in contract.Endpoints.OrderBy(static endpoint => endpoint.Name, StringComparer.Ordinal))
        {
            builder.Append("    Task<").Append(endpoint.Response!.Name).Append("> ")
                .Append(endpoint.Name).Append("Async(")
                .Append(endpoint.Request!.Name).Append(" request, CancellationToken cancellationToken = default);")
                .AppendLine();
        }

        builder.AppendLine("}");
        builder.AppendLine();

        builder.Append("public sealed class ").Append(contract.ClientName).Append("(HttpClient httpClient) : ")
            .Append(interfaceName).AppendLine();
        builder.AppendLine("{");

        foreach (EndpointModel endpoint in contract.Endpoints.OrderBy(static endpoint => endpoint.Name, StringComparer.Ordinal))
        {
            builder.Append("    public async Task<").Append(endpoint.Response!.Name).Append("> ")
                .Append(endpoint.Name).Append("Async(")
                .Append(endpoint.Request!.Name).Append(" request, CancellationToken cancellationToken = default)")
                .AppendLine();
            builder.AppendLine("    {");
            builder.Append("        using HttpRequestMessage message = new(new HttpMethod(\"")
                .Append(endpoint.Method.ToUpperInvariant())
                .Append("\"), \"")
                .Append(endpoint.Route)
                .AppendLine("\");");

            if (!endpoint.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine("        message.Content = JsonContent.Create(request);");
            }

            builder.AppendLine("        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);");
            builder.AppendLine("        response.EnsureSuccessStatusCode();");
            builder.Append("        ").Append(endpoint.Response.Name).AppendLine("? payload = await response.Content.ReadFromJsonAsync<")
                .Append(endpoint.Response.Name)
                .AppendLine(">(cancellationToken: cancellationToken).ConfigureAwait(false);");
            builder.AppendLine("        return payload ?? throw new InvalidOperationException(\"Response payload was null.\");");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void EmitPayload(StringBuilder builder, EndpointPayloadModel payload)
    {
        builder.Append("public sealed class ").Append(payload.Name).AppendLine();
        builder.AppendLine("{");

        foreach (KeyValuePair<string, string> property in payload.Properties.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            builder.Append("    public ").Append(property.Value).Append(' ').Append(property.Key).Append(" { get; init; }");
            if (property.Value.Equals("string", StringComparison.Ordinal))
            {
                builder.Append(" = string.Empty;");
            }

            builder.AppendLine();
        }

        builder.AppendLine("}");
        builder.AppendLine();
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName,
        DiagnosticDescriptor descriptor,
        List<Diagnostic> diagnostics)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(Diagnostic.Create(descriptor, Location.None));
            return string.Empty;
        }

        string value = property.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(Diagnostic.Create(descriptor, Location.None));
            return string.Empty;
        }

        return value;
    }

    private static bool IsAllowedMethod(string method)
    {
        return method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            || method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            || method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToUpperInvariant();
        }

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private sealed class ContractModel
    {
        public ContractModel(string clientName, string targetNamespace, IReadOnlyList<EndpointModel> endpoints)
        {
            ClientName = clientName;
            TargetNamespace = targetNamespace;
            Endpoints = endpoints;
        }

        public string ClientName { get; }

        public string TargetNamespace { get; }

        public IReadOnlyList<EndpointModel> Endpoints { get; }
    }

    private sealed class EndpointModel
    {
        public EndpointModel(
            string name,
            string method,
            string route,
            EndpointPayloadModel? request,
            EndpointPayloadModel? response)
        {
            Name = name;
            Method = method;
            Route = route;
            Request = request;
            Response = response;
        }

        public string Name { get; }

        public string Method { get; }

        public string Route { get; }

        public EndpointPayloadModel? Request { get; }

        public EndpointPayloadModel? Response { get; }
    }

    private sealed class EndpointPayloadModel
    {
        public EndpointPayloadModel(string name, IReadOnlyDictionary<string, string> properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, string> Properties { get; }
    }

    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor MissingContractFile = new(
            id: "STA000",
            title: "Missing API contract",
            messageFormat: "The contract file 'api-contract.json' was not provided as an AdditionalFile.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidContractJson = new(
            id: "STA001",
            title: "Invalid contract JSON",
            messageFormat: "The API contract is invalid JSON: {0}",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingClientName = new(
            id: "STA002",
            title: "Missing clientName",
            messageFormat: "The contract must define a non-empty 'clientName'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingNamespace = new(
            id: "STA003",
            title: "Missing namespace",
            messageFormat: "The contract must define a non-empty 'namespace'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingEndpoints = new(
            id: "STA004",
            title: "Missing endpoints",
            messageFormat: "The contract must include an 'endpoints' array.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingEndpointName = new(
            id: "STA005",
            title: "Missing endpoint name",
            messageFormat: "Each endpoint must define a non-empty 'name'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingHttpMethod = new(
            id: "STA006",
            title: "Missing HTTP method",
            messageFormat: "Each endpoint must define a valid 'method'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidHttpMethod = new(
            id: "STA007",
            title: "Invalid HTTP method",
            messageFormat: "Unsupported HTTP method '{0}'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingRoute = new(
            id: "STA008",
            title: "Missing route",
            messageFormat: "Each endpoint must define a non-empty 'route'.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingRequest = new(
            id: "STA009",
            title: "Missing request",
            messageFormat: "Each endpoint must define a valid 'request' object with name and properties.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingResponse = new(
            id: "STA010",
            title: "Missing response",
            messageFormat: "Each endpoint must define a valid 'response' object with name and properties.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidPropertyType = new(
            id: "STA011",
            title: "Invalid payload property type",
            messageFormat: "Payload property '{0}' must have a non-empty string type.",
            category: "StronglyTypedApiClient",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
