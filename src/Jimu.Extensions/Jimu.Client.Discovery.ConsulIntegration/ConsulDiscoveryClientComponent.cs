﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Consul;
using Jimu.Logger;
using Microsoft.Extensions.Configuration;

namespace Jimu.Client.Discovery.ConsulIntegration
{
    public class ConsulDiscoveryClientComponent : ClientComponentBase
    {
        readonly ConsulOptions _options;
        public ConsulDiscoveryClientComponent(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(ConsulOptions).Name).Get<ConsulOptions>();
        }

        public override void DoInit(IContainer container)
        {
            if (_options != null)
            {
                var logger = container.Resolve<ILogger>();
                logger.Info($"[config]use consul for services discovery, consul ip: {_options.Ip}:{_options.Port}, service cateogry: {_options.ServiceGroups}");

                var clientDiscovery = container.Resolve<IClientServiceDiscovery>();
                clientDiscovery.AddRoutesGetter(async () =>
                {
                    var consul = new ConsulClient(config => { config.Address = new Uri($"http://{_options.Ip}:{_options.Port}"); });
                    HashSet<string> keyset = new HashSet<string>();
                    foreach (var group in _options.ServiceGroups.Split(','))
                    {
                        if (string.IsNullOrEmpty(group)) continue;
                        var queryResult = await consul.KV.Keys(group);
                        if (queryResult == null || queryResult.Response == null) continue;

                        foreach (var key in queryResult.Response)
                        {
                            keyset.Add(key);
                        }
                    }
                    if (!keyset.Any())
                    {
                        return null;
                    }

                    var routes = new List<JimuServiceRoute>();
                    foreach (var key in keyset)
                    {
                        var data = (await consul.KV.Get(key)).Response?.Value;
                        if (data == null)
                        {
                            continue;
                        }

                        var descriptors = JimuHelper.Deserialize<byte[], List<JimuServiceRouteDesc>>(data);
                        if (descriptors != null && descriptors.Any())
                        {
                            foreach (var descriptor in descriptors)
                            {
                                List<JimuAddress> addresses =
                                    new List<JimuAddress>(descriptor.AddressDescriptors.ToArray().Count());
                                foreach (var addDesc in descriptor.AddressDescriptors)
                                {
                                    //var addrType = Type.GetType(addDesc.Type);
                                    addresses.Add(JimuHelper.Deserialize(addDesc.Value, typeof(JimuAddress)) as JimuAddress);
                                }

                                routes.Add(new JimuServiceRoute
                                {
                                    Address = addresses,
                                    ServiceDescriptor = descriptor.ServiceDescriptor
                                });
                            }
                        }

                    }

                    return routes;
                });
            }
            base.DoInit(container);
        }


    }
}
