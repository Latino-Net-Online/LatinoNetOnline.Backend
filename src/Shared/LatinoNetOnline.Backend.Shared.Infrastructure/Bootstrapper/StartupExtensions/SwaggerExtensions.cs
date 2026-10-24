
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;

namespace LatinoNetOnline.Backend.Shared.Infrastructure.Bootstrapper.StartupExtensions
{
    public static class SwaggerExtensions
    {
        public static SwaggerGenOptions BearerSecurity(this SwaggerGenOptions options)
        {
            var securityScheme = new OpenApiSecurityScheme
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


            return options;
        }


        public static SwaggerGenOptions OAuth2Security(this SwaggerGenOptions options)
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://ids.latinonet.online/connect/authorize"),
                        TokenUrl = new Uri("https://ids.latinonet.online/connect/token"),
                        Scopes = new Dictionary<string, string>
                            {
                                {"latinonetonline_api", "latinonetonline_api"},
                                {"roles", "roles"},
                                {"openid", "openid"}
                            }
                    }
                }
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement()
                {
                    [new OpenApiSecuritySchemeReference("oauth2", document)] = new List<string>()
                });


            return options;
        }


        public static SwaggerGenOptions BasicSecurity(this SwaggerGenOptions options)
        {
            options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                In = ParameterLocation.Header,
                Description = "Basic Authorization header using the Bearer scheme."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("basic", document)] = new List<string>()
            });



            return options;
        }
    }
}