
#if NET9_0_OR_GREATER

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore.Annotations.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;


namespace Scalar.AspNetCore.Annotations.Native;

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

            if (scalarOp.Tags != null && scalarOp.Tags.Any())
            {
                operation.Tags = scalarOp.Tags.Select(t => new OpenApiTag { Name = t }).ToList();
            }

            if (!string.IsNullOrEmpty(scalarOp.ThemeColor))
            {
                // In Scalar, color isn't a native property, so we use their recognized x-extension or tag it
                operation.Extensions["x-scalar-color"] = new OpenApiString(scalarOp.ThemeColor);
            }
        }

        var scalarResponse = metadata.OfType<ScalarResponseAttribute>().ToList();

        if ( scalarResponse != null)
        {
            scalarResponse.ForEach(r =>
            {
                var statusCode = r.StatusCode.ToString();
                if (operation.Responses.ContainsKey(statusCode))
                {
                    var response = operation.Responses[statusCode];
                    if (!string.IsNullOrEmpty(r.Description))
                    {
                        response.Description = r.Description;
                    }

                    if(!string.IsNullOrEmpty(r.ContentTypes?.FirstOrDefault()))
                    {
                        response.Content = r.ContentTypes.ToDictionary(ct => ct, ct => new OpenApiMediaType());
                    }
                }
            });
        }

        // 1. Handle Badges
        var badges = metadata.OfType<ScalarBadgeAttribute>().ToList();
        if (badges.Any())
        {
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
        }

        // 2. Handle Code Samples
        var codeSamples = metadata.OfType<ScalarCodeSampleAttribute>().ToList();
        if (codeSamples.Any())
        {
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
        }

        // 3. Handle Exclusions
        if (metadata.OfType<ScalarExcludeAttribute>().Any())
        {
            operation.Extensions["x-scalar-ignore"] = new OpenApiBoolean(true);
        }

        return Task.CompletedTask;
    }
}
#endif