using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace lockhaven_backend.Filters;

public class FileUploadOperation : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if any parameter is IFormFile
        var hasFileParameter = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Type == typeof(IFormFile) || 
                     (p.ModelMetadata != null && p.ModelMetadata.ModelType == typeof(IFormFile)));

        if (!hasFileParameter)
            return;

        // Clear existing parameters
        operation.Parameters.Clear();

        // Set up multipart/form-data request body
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "The file to upload"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}

