
#if NET9_0_OR_GREATER

using Artkinx.ScalarAspNetCore.Annotations.Attributes;
using Microsoft.AspNetCore.OpenApi;
using System.Text.Json.Nodes;


#if NET9_0
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
#elif NET10_0_OR_GREATER
using Microsoft.OpenApi;
#endif


namespace Artkinx.ScalarAspNetCore.Annotations.Native;

/// <summary>
/// Transforms OpenAPI operations by inspecting endpoint metadata for Scalar-specific attributes and injecting corresponding OpenAPI extensions that Swashbuckle can utilize to enhance the generated documentation with badges, code samples, and exclusion flags.
/// </summary>
public class ScalarOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>
    /// Transforms the given OpenAPI operation by extracting Scalar-specific attributes from the endpoint metadata and adding appropriate extensions to the operation definition. This allows Swashbuckle to recognize and render badges, code samples, and exclusion flags in the generated API documentation.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Extract all attributes from the endpoint metadata
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;


        // 1. Handle ScalarOperationAttribute for general operation metadata
        var scalarOp = metadata.OfType<ScalarOperationAttribute>().FirstOrDefault();

        if (scalarOp != null)
        {
            if (!string.IsNullOrEmpty(scalarOp.Summary))
                operation.Summary = scalarOp.Summary;

            if (!string.IsNullOrEmpty(scalarOp.Description))
                operation.Description = scalarOp.Description;

            if (!string.IsNullOrEmpty(scalarOp.OperationId))
                operation.OperationId = scalarOp.OperationId;

            if (scalarOp.Tags?.Any() is true)
            {
#if NET10_0 
                operation.Tags = (ISet<OpenApiTagReference>?)scalarOp.Tags.Select(selector: t => new OpenApiTagReference(referenceId: t)).ToList();
#elif NET9_0
                operation.Tags = [.. scalarOp.Tags.Select(selector: t => new OpenApiTag() { Name = t })];
#endif
            }

            if (!string.IsNullOrEmpty(scalarOp.ThemeColor))
            {
                // In Scalar, color isn't a native property, so we use their recognized x-extension or tag it
#if NET9_0
                operation.Extensions["x-scalar-color"] = new OpenApiString(scalarOp.ThemeColor);

#elif  NET10_0
                operation.Extensions?["x-scalar-color"] = new JsonNodeExtension(JsonValue.Create(scalarOp.ThemeColor));
#endif
            }
        }

        var scalarResponse = metadata.OfType<ScalarResponseAttribute>().ToList();

#if NET9_0 || NET10_0
        scalarResponse?.ForEach(r =>
            {
                var statusCode = r.StatusCode.ToString();
                if (operation.Responses.ContainsKey(statusCode))
                {
                    var response = operation.Responses[statusCode];
                    if (!string.IsNullOrEmpty(r.Description))
                    {
                        response.Description = r.Description;
                    }

                    if (!string.IsNullOrEmpty(r.ContentTypes?.FirstOrDefault()))
                    {
                        foreach (var ct in r.ContentTypes)
                        {
                            if (!response.Content.ContainsKey(ct))
                            {
                                response.Content[ct] = new OpenApiMediaType();
                            }
                        }
                        //response.Content = r.ContentTypes.ToDictionary(ct => ct, ct => new OpenApiMediaType());
                    }
                }
            });
#endif
        // 1. Handle Badges
        var badges = metadata.OfType<ScalarBadgeAttribute>().ToList();
        if (badges.Count > 0)
        {
#if NET8_0
            var badgeArray = new OpenApiArray();
            foreach (var badge in badges)
            {
                // Injecting Scalar's expected x- extension format
                badgeArray.Add(new OpenApiObject
                {
                    ["name"] = new OpenApiString(badge.Name),
                    ["position"] = new OpenApiString(badge.Position.ToString().ToLowerInvariant()),
                    ["color"] = new OpenApiString(badge.Color)
                });
            }
            operation.Extensions["x-badges"] = badgeArray;
#endif
        }

        // 2. Handle Code Samples
        var codeSamples = metadata.OfType<ScalarCodeSampleAttribute>().ToList();
        if (codeSamples.Count > 0)
        {
#if NET8_0
            var sampleArray = new OpenApiArray();
            foreach (var sample in codeSamples)
            {
                sampleArray.Add(new OpenApiObject
                {
                    ["lang"] = new OpenApiString(sample.Language),
                    ["source"] = new OpenApiString(sample.Code),
                    ["label"] = new OpenApiString(string.IsNullOrEmpty(sample.Title) ? sample.Language : sample.Title)
                });
            }
            operation.Extensions["x-codeSamples"] = sampleArray;
#endif
        }

        // 3. Handle Exclusions
        if (metadata.OfType<ScalarExcludeAttribute>().Any())
        {
#if NET8_0
            operation.Extensions["x-scalar-ignore"] = new OpenApiBoolean(true);
#endif
        }

        return Task.CompletedTask;
    }
}
#endif



