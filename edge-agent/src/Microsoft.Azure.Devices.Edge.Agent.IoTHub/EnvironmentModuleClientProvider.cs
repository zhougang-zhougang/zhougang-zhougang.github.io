// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Agent.IoTHub
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Edge.Agent.Core;
    using Microsoft.Azure.Devices.Edge.Util;

    public class EnvironmentModuleClientProvider : IModuleClientProvider
    {
        readonly Option<UpstreamProtocol> upstreamProtocol;
        readonly Option<IWebProxy> proxy;
        readonly Option<string> productInfo;
        readonly ITokenProvider tokenProvider;
        readonly string iothub;
        readonly string deviceId;
        readonly string moduleId;

        public EnvironmentModuleClientProvider(
            Option<UpstreamProtocol> upstreamProtocol,
            Option<IWebProxy> proxy,
            Option<string> productInfo,
            ITokenProvider tokenProvider,
            string iothub,
            string deviceId,
            string moduleId)
        {
            this.upstreamProtocol = upstreamProtocol;
            this.proxy = proxy;
            this.productInfo = productInfo;
            this.tokenProvider = tokenProvider;
            this.deviceId = deviceId;
            this.iothub = iothub;
            this.moduleId = moduleId;
        }

        public Task<IModuleClient> Create(
            ConnectionStatusChangesHandler statusChangedHandler,
            Func<IModuleClient, Task> initialize) =>
            ModuleClient.Create(
                Option.None<string>(),
                this.upstreamProtocol,
                statusChangedHandler,
                initialize,
                this.proxy,
                this.productInfo,
                this.tokenProvider,
                this.iothub,
                this.deviceId,
                this.moduleId);
    }
}
