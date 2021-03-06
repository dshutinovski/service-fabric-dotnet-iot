﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Iot.Admin.WebService
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using IoT.Common;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    internal sealed class WebService : StatelessService
    {
        private readonly CancellationTokenSource webApiCancellationSource;
        private readonly FabricClient fabricClient;

        public WebService(StatelessServiceContext context)
            : base(context)
        {
            this.webApiCancellationSource = new CancellationTokenSource();
            this.fabricClient = new FabricClient();
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[1]
            {
                new ServiceInstanceListener(
                    context =>
                    {
                        return new WebHostCommunicationListener(
                            context,
                            "iot",
                            "ServiceEndpoint",
                            uri =>
                            {
                                ServiceEventSource.Current.Message($"Admin WebService starting on {uri}");

                                return new WebHostBuilder().UseWebListener()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<FabricClient>(this.fabricClient)
                                            .AddSingleton<CancellationTokenSource>(this.webApiCancellationSource))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseUrls(uri)
                                    .Build();
                            });
                    })
            };
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => this.webApiCancellationSource.Cancel());

            return Task.FromResult(true);
        }
    }
}