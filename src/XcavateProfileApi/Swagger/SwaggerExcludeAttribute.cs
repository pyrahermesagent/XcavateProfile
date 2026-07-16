using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace XcavateProfileApi.Swagger;

/// <summary>
/// Attribute to exclude an action from Swagger documentation
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SwaggerExcludeAttribute : Attribute
{
}

/// <summary>
/// Operation filter that excludes operations with SwaggerExclude attribute
/// </summary>
public class ExcludeSwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttributes(typeof(SwaggerExcludeAttribute), true).Length > 0)
        {
            // Mark as deprecated so it's hidden or clearly marked as not recommended
            operation.Deprecated = true;
        }
    }
}
