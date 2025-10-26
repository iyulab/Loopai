using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Loopai.CloudApi.Swagger;

/// <summary>
/// Schema filter to add examples to Swagger documentation.
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.Name == "CreateTaskRequest")
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("calculate_tax"),
                ["description"] = new OpenApiString("Calculate sales tax for a given amount"),
                ["input_schema"] = new OpenApiObject
                {
                    ["type"] = new OpenApiString("object"),
                    ["properties"] = new OpenApiObject
                    {
                        ["amount"] = new OpenApiObject
                        {
                            ["type"] = new OpenApiString("number")
                        },
                        ["state"] = new OpenApiObject
                        {
                            ["type"] = new OpenApiString("string")
                        }
                    },
                    ["required"] = new OpenApiArray
                    {
                        new OpenApiString("amount"),
                        new OpenApiString("state")
                    }
                },
                ["output_schema"] = new OpenApiObject
                {
                    ["type"] = new OpenApiString("object"),
                    ["properties"] = new OpenApiObject
                    {
                        ["tax"] = new OpenApiObject
                        {
                            ["type"] = new OpenApiString("number")
                        },
                        ["total"] = new OpenApiObject
                        {
                            ["type"] = new OpenApiString("number")
                        }
                    },
                    ["required"] = new OpenApiArray
                    {
                        new OpenApiString("tax"),
                        new OpenApiString("total")
                    }
                },
                ["examples"] = new OpenApiArray(),
                ["accuracy_target"] = new OpenApiDouble(0.95),
                ["latency_target_ms"] = new OpenApiInteger(100),
                ["sampling_rate"] = new OpenApiDouble(0.1)
            };
        }
        else if (context.Type.Name == "ExecuteRequest")
        {
            schema.Example = new OpenApiObject
            {
                ["task_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["input"] = new OpenApiObject
                {
                    ["amount"] = new OpenApiDouble(100.0),
                    ["state"] = new OpenApiString("CA")
                },
                ["version"] = new OpenApiInteger(1),
                ["timeout_ms"] = new OpenApiInteger(5000)
            };
        }
        else if (context.Type.Name == "CompareProgramsRequest")
        {
            schema.Example = new OpenApiObject
            {
                ["control_program_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["treatment_program_id"] = new OpenApiString("4gb96g75-6828-5673-c4gd-3d074g77bgb7"),
                ["configuration"] = new OpenApiObject
                {
                    ["minimum_sample_size"] = new OpenApiInteger(100),
                    ["required_confidence"] = new OpenApiDouble(0.95),
                    ["max_degradation_threshold"] = new OpenApiDouble(0.05),
                    ["min_improvement_threshold"] = new OpenApiDouble(0.02),
                    ["test_duration_hours"] = new OpenApiInteger(24)
                }
            };
        }
        else if (context.Type.Name == "StartCanaryRequest")
        {
            schema.Example = new OpenApiObject
            {
                ["task_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["new_program_id"] = new OpenApiString("4gb96g75-6828-5673-c4gd-3d074g77bgb7")
            };
        }
    }
}
