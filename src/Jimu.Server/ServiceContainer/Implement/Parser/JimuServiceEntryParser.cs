﻿using Autofac;
using Autofac.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jimu.Server.Implement.Parser
{
    class JimuServiceEntryParser
    {
        private readonly IContainer _container;
        private readonly ConcurrentDictionary<Tuple<Type, string>, FastInvoke.FastInvokeHandler> _handler;
        public JimuServiceEntryParser(IContainer container)
        {
            _container = container;
        }

        public JimuServiceEntry Parse(MethodInfo methodInfo, JimuServiceDesc desc)
        {
            var fastInvoker = FastInvoke.GetMethodInvoker(methodInfo);

            var service = new JimuServiceEntry
            {
                Descriptor = desc,
                Func = (paras, payload) =>
                {
                    var instance = GetInstance(null, methodInfo.DeclaringType, payload);
                    var parameters = new List<object>();
                    foreach (var para in methodInfo.GetParameters())
                    {
                        paras.TryGetValue(para.Name, out var value);
                        var paraType = para.ParameterType;
                        var parameter = JimuHelper.ConvertType(value, paraType);
                        parameters.Add(parameter);
                    }

                    var result = fastInvoker(instance, parameters.ToArray());
                    return Task.FromResult(result);
                }
            };
            return service;
        }

        //private FastInvoke.FastInvokeHandler GetInvokeHandler(string key, MethodInfo method)
        //{
        //_handler.TryGetValue(Tuple.Create(method.DeclaringType, key), out var handler);
        //if (handler == null)
        //{
        //    handler = FastInvoke.GetMethodInvoker(method);
        //    _handler.GetOrAdd(Tuple.Create(method.DeclaringType, key), handler);
        //}
        //return handler;

        //}

        private object GetInstance(string key, Type type, JimuPayload payload)
        {
            // all service are instancePerDependency, to avoid resolve the same isntance , so we add using scop here
            using (var scope = _container.BeginLifetimeScope())
            {
                if (string.IsNullOrEmpty(key))
                    return scope.Resolve(type,
                        new ResolvedParameter(
                            (pi, ctx) => pi.ParameterType == typeof(JimuPayload),
                            (pi, ctx) => payload
                        ));
                return scope.ResolveKeyed(key, type,
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(JimuPayload),
                        (pi, ctx) => payload
                    ));
            }
        }

    }
}
