﻿using System;
using Autofac;
using Jimu;
using Jimu.Logger;
using Jimu.Server;

namespace Server2.HttpForTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();
            var builder = new ApplicationServerBuilder(containerBuilder)
                //.UseLog4netLogger(new JimuLog4netOptions
                //{
                //    EnableConsoleLog = true
                //})
                //.LoadServices(new[] { "IServices", "Services" })
                //.UseHttpForTransfer(new Jimu.Server.Transport.Http.HttpOptions("127.0.0.1", 8008))
                //.UseConsulForDiscovery(new Jimu.Server.Discovery.ConsulIntegration.ConsulOptions("127.0.0.1", 8500, "JimuService-", "127.0.0.1:8008"))
                ;
            using (var hostJimu = builder.Build())
            {
                hostJimu.Run();
                Console.ReadLine();
            }

        }
    }
}
