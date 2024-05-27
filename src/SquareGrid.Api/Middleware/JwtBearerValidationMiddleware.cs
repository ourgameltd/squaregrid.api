using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using SquareGrid.Api.Middleware.Tokens;

namespace SquareGrid.Api;

internal sealed class JwtBearerValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly B2CConfigurationManager configurationManager;

    public JwtBearerValidationMiddleware(B2CConfigurationManager configurationManager)
    {
        this.configurationManager = configurationManager;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();

        if (requestData!.Headers.TryGetValues("Authorization", out var values))
        {
            try
            {
                var token = values.First().Replace("Bearer ", "");
                var user = await configurationManager.ValidateToken(token);
                context.Items.Add(nameof(ClaimsPrincipal), user);
            }
            catch (Exception)
            {
                var res = requestData!.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("No Authorization header provided.");
                context.GetInvocationResult().Value = res;
                return;
            }
        }
        else
        {
            var res = requestData!.CreateResponse(HttpStatusCode.Unauthorized);
            context.GetInvocationResult().Value = res;
            return;
        }

        await next(context);
    }

    public static bool IsAuthorizedHttp(FunctionContext context)
    {
        MethodInfo GetTargetFunctionMethod(FunctionContext context)
        {
            var entryPoint = context.FunctionDefinition.EntryPoint;

            var assemblyPath = context.FunctionDefinition.PathToAssembly;
            var assembly = Assembly.LoadFrom(assemblyPath);
            var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
            var type = assembly.GetType(typeName);
            var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
            var method = type!.GetMethod(methodName);
            return method!;
        }

        var httpTrigger = context.FunctionDefinition.InputBindings.Values.First(a => a.Type.EndsWith("Trigger") && a.Type == "httpTrigger");

        if (httpTrigger == null)
        {
            return false;
        }

        var method = GetTargetFunctionMethod(context);
        var authAttribute = method.GetCustomAttribute<AuthorizeAttribute>();

        if (authAttribute == null)
        {
            return false;
        }

        return true;
    }
}