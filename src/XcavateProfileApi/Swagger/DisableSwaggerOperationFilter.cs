using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace XcavateProfileApi.Swagger;

/// <summary>
/// Operation filter to handle IFormFile parameters in Swagger generation.
/// When an action has an IFormFile parameter, we need to modify the operation
/// to properly handle multipart/form-data content.
/// </summary>
public class DisableSwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFormFileParameter = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile));

        if (hasFormFileParameter)
        {
            // For IFormFile operations, we need to change the request body to
            // multipart/form-data instead of application/json
            // Remove any existing request body
            operation.RequestBody = null;

            // Add a parameter for the file - use OpenApiParameter directly
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "image",
                In = Microsoft.OpenApi.Models.ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                }
            });
        }
    }
}
