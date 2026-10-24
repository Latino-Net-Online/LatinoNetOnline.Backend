
using LatinoNetOnline.Backend.Shared.Infrastructure.Bootstrapper.Swagger;

using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LatinoNetOnline.Backend.Shared.Infrastructure.Bootstrapper.StartupExtensions
{
    public static class ApiVersioningExtensions
    {
        public static IServiceCollection AddSwaggerApiVersioning(this IServiceCollection services, Assembly assembly)
        {
            services.AddApiVersioning(
                 options =>
                 {
                     // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                     options.ReportApiVersions = true;
                     options.DefaultApiVersion = new(1, 0);
                     options.AssumeDefaultVersionWhenUnspecified = true;
                 })
                .AddMvc()
                .AddApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";
                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {

                    options.OAuth2Security();
                    OpenApiSecurityScheme securityScheme = new()
                    {
                        Name = "JWT Authentication",
                        Description = "Enter JWT Bearer token **_only_**",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer", // must be lower case
                        BearerFormat = "JWT"
                    };

                    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

                    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = new List<string>()
                    });

                    // add a custom operation filter which sets default values
                    options.OperationFilter<SwaggerDefaultValues>();

                    // integrate xml commentsi
                    options.IncludeXmlComments(GetXmlCommentsFilePath(assembly));
                });

            return services;
        }

        public static IApplicationBuilder UseSwaggerApiVersioning(this IApplicationBuilder app, IApiVersionDescriptionProvider provider, IConfiguration configuration)
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>

                {
                    swagger.Servers = new List<OpenApiServer> { new() { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } };
                });
            });

            app.UseSwaggerUI(
            options =>
            {
                options.DisplayOperationId();
                options.DisplayRequestDuration();
                options.OAuthUsePkce();
                options.OAuthClientId(configuration["Security:SwaggerClientId"]);
                options.OAuthClientSecret(configuration["Security:SwaggerClientSecret"]);


                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            return app;
        }

        static string GetXmlCommentsFilePath(Assembly assembly)
        {
            var basePath = System.AppContext.BaseDirectory;
            var fileName = assembly.GetName().Name + ".xml";
            return Path.Combine(basePath, fileName);
        }
    }
}
