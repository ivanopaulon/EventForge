using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using EventForge.Server.Controllers;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the method has a ChatFileUploadRequestDto parameter
        var hasFileUploadDto = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(ChatFileUploadRequestDto));
        
        // Also check for direct IFormFile parameters for other endpoints
        var hasDirectFileParams = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile));

        if (hasFileUploadDto || hasDirectFileParams)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File to upload"
                                },
                                ["chatId"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Chat identifier where the file will be uploaded"
                                }
                            },
                            Required = new HashSet<string> { "file", "chatId" }
                        }
                    }
                }
            };
        }
    }
}