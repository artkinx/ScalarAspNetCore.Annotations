using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore.Annotations.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;


namespace Scalar.AspNetCore.Annotations.Swashbuckle;

/// <summary>
/// Implements a Swashbuckle operation filter that inspects API endpoint metadata for Scalar-specific attributes such as <see cref="ScalarOperationAttribute"/>, <see cref="ScalarExcludeAttribute"/>, and <see cref="ScalarCodeSampleAttribute"/>. This filter enriches the OpenAPI operation definitions with corresponding extensions that Swashbuckle can utilize to enhance the generated API documentation with badges, code samples, and exclusion flags, allowing for improved visualization and organization in the Scalar UI. By applying this filter, developers can easily integrate Scalar-specific metadata into their API documentation by simply adding the appropriate attributes to their API endpoints.
/// </summary>
public class ScalarSwashbuckleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 1. Handle ScalarOperationAttribute
        var scalarOperation = context.MethodInfo.GetCustomAttribute<ScalarOperationAttribute>();
        if (scalarOperation != null)
        {
            if (!string.IsNullOrEmpty(scalarOperation.Summary))
                operation.Summary = scalarOperation.Summary;

            if (!string.IsNullOrEmpty(scalarOperation.Description))
                operation.Description = scalarOperation.Description;

            if (!string.IsNullOrEmpty(scalarOperation.OperationId))
                operation.OperationId = scalarOperation.OperationId;

            if (scalarOperation.Tags != null && scalarOperation.Tags.Any())
            {
                operation.Tags = scalarOperation.Tags.Select(t => new OpenApiTag { Name = t }).ToList();
            }

            if (!string.IsNullOrEmpty(scalarOperation.ThemeColor))
            {
                operation.Extensions["x-scalar-color"] = new OpenApiString(scalarOperation.ThemeColor);
            }
        }

        // 2. Handle ScalarExcludeAttribute
        var excludeAttribute = context.MethodInfo.GetCustomAttribute<ScalarExcludeAttribute>();
        if (excludeAttribute != null)
        {
            operation.Extensions["x-scalar-ignore"] = new OpenApiBoolean(true);
        }

        // 3. Handle ScalarCodeSampleAttribute
        var codeSamples = context.MethodInfo.GetCustomAttributes<ScalarCodeSampleAttribute>().ToList();
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
    }
}
