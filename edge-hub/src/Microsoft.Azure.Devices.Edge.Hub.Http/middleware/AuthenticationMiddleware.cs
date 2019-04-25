// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Hub.Http.Middleware
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Azure.Devices.Edge.Hub.Core;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Identity;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using static System.FormattableString;

    public class AuthenticationMiddleware
    {
        readonly RequestDelegate next;
        readonly Task<IAuthenticator> authenticatorTask;
        readonly IClientCredentialsFactory identityFactory;
        readonly string iotHubName;
        readonly string edgeDeviceId;

        public AuthenticationMiddleware(
            RequestDelegate next,
            Task<IAuthenticator> authenticatorTask,
            IClientCredentialsFactory identityFactory,
            string iotHubName,
            string edgeDeviceId)
        {
            this.next = next;
            this.authenticatorTask = Preconditions.CheckNotNull(authenticatorTask, nameof(authenticatorTask));
            this.identityFactory = Preconditions.CheckNotNull(identityFactory, nameof(identityFactory));
            this.iotHubName = Preconditions.CheckNonWhiteSpace(iotHubName, nameof(iotHubName));
            this.edgeDeviceId = Preconditions.CheckNonWhiteSpace(edgeDeviceId, nameof(edgeDeviceId));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                (bool isAuthenticated, string errorMessage) result = await this.AuthenticateRequest(context);
                if (result.isAuthenticated)
                {
                    await this.next.Invoke(context);
                }
                else
                {
                    await WriteErrorResponse(context, result.errorMessage);
                }
            }
            catch (Exception ex)
            {
                Events.AuthenticationError(ex, context);
                throw;
            }
        }

        internal async Task<(bool, string)> AuthenticateRequest(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(HttpConstants.IdHeaderKey, out StringValues clientIds) || clientIds.Count == 0)
            {
                Console.WriteLine("Request header does not contain ModuleId");
            }

            Console.WriteLine($"Current request url is - {context.Request.GetDisplayUrl()}");

            string clientId = clientIds.FirstOrDefault() ?? "rpi2/Sender";
            string[] clientIdParts = clientId.Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (clientIdParts.Length != 2)
            {
                return LogAndReturnFailure("Id header doesn't contain device Id and module Id as expected.");
            }

            string deviceId = clientIdParts[0];
            string moduleId = clientIdParts[1];

            IClientCredentials clientCredentials = this.identityFactory.GetWithSasToken(deviceId, moduleId, string.Empty, Guid.NewGuid().ToString(), false);
            IIdentity identity = clientCredentials.Identity;

            context.Items.Add(HttpConstants.IdentityKey, identity);
            Events.AuthenticationSucceeded(identity);
            await Task.CompletedTask;
            return (true, string.Empty);
        }

        static (bool, string) LogAndReturnFailure(string message, Exception ex = null)
        {
            Events.AuthenticationFailed(message, ex);
            return (false, message);
        }

        static async Task WriteErrorResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(message);
        }

        static class Events
        {
            const int IdStart = HttpEventIds.AuthenticationMiddleware;
            static readonly ILogger Log = Logger.Factory.CreateLogger<AuthenticationMiddleware>();

            enum EventIds
            {
                AuthenticationFailed = IdStart,
                AuthenticationError,
                AuthenticationSuccess
            }

            public static void AuthenticationFailed(string message, Exception ex)
            {
                if (ex == null)
                {
                    Log.LogDebug((int)EventIds.AuthenticationFailed, Invariant($"Http Authentication failed due to following issue - {message}"));
                }
                else
                {
                    Log.LogWarning((int)EventIds.AuthenticationFailed, ex, Invariant($"Http Authentication failed due to following issue - {message}"));
                }
            }

            public static void AuthenticationError(Exception ex, HttpContext context)
            {
                // TODO - Check if it is okay to put request headers in logs.
                Log.LogError((int)EventIds.AuthenticationError, ex, Invariant($"Unknown error occurred during authentication, for request with headers - {context.Request.Headers}"));
            }

            public static void AuthenticationSucceeded(IIdentity identity)
            {
                Log.LogDebug((int)EventIds.AuthenticationSuccess, Invariant($"Http Authentication succeeded for device with Id {identity.Id}"));
            }
        }
    }

    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder, string iotHubName, string edgeDeviceId)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>(iotHubName, edgeDeviceId);
        }
    }
}
