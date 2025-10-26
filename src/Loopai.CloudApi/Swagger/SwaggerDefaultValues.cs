using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Loopai.CloudApi.Swagger;

/// <summary>
/// Swagger operation filter to set default values for API versioning.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
        {
            return;
        }

        var apiDescription = context.ApiDescription;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description != null)
            {
                parameter.Description ??= description.ModelMetadata?.Description;
                parameter.Required |= description.IsRequired;
            }
        }
    }
}
