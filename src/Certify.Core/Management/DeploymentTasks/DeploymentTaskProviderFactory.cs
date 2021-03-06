﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certify.Config;
using Certify.Models;
using Certify.Models.Config;
using Certify.Models.Plugins;
using Certify.Models.Providers;
using Certify.Providers.DeploymentTasks;

namespace Certify.Core.Management.DeploymentTasks
{
    public static class DeploymentTaskProviderFactory
    {
        public async static Task<List<DeploymentProviderDefinition>> GetDeploymentTaskProviders(List<IDeploymentTaskProviderPlugin> providerPlugins)
        {
            var list = new List<DeploymentProviderDefinition>();

            var baseAssembly = typeof(DeploymentTaskProviderFactory).Assembly;

            // we filter the defined classes according to the interfaces they implement
            var typeList = baseAssembly.GetTypes().Where(type => type.GetInterfaces().Any(inter => inter == typeof(IDeploymentTaskProvider))).ToList();

            foreach (var t in typeList)
            {
                var def = (DeploymentProviderDefinition)t.GetProperty("Definition").GetValue(null);

#if DEBUG
                // conditionally include mock task
                if (def.Id == Providers.DeploymentTasks.Core.MockTask.Definition.Id)
                {
                    list.Add(def);
                }
                else
                {
                    list.Add(def);
                }
#else
                list.Add(def);
#endif
            }

            // add providers from plugins (if any)
            if (providerPlugins == null)
            {
                return list;
            }

            foreach (var p in providerPlugins)
            {
                if (p != null)
                {
                    list.AddRange(p.GetProviders());
                }
            }

            return await Task.FromResult(list);
        }

        public static IDeploymentTaskProvider Create(string taskTypeId, List<IDeploymentTaskProviderPlugin> providerPlugins)
        {
            if (taskTypeId == null)
            {
                return null;
            }

            taskTypeId = taskTypeId.ToLower();

            // find the provider in our current assembly

            var baseAssembly = typeof(DeploymentTaskProviderFactory).Assembly;
            var typeList = baseAssembly.GetTypes().Where(type => type.GetInterfaces().Any(inter => inter == typeof(IDeploymentTaskProvider))).ToList();

            foreach (var t in typeList)
            {
                var def = (DeploymentProviderDefinition)t.GetProperty("Definition").GetValue(null);
                if (def.Id.ToLower() == taskTypeId)
                {
                    return (IDeploymentTaskProvider)Activator.CreateInstance(t);
                }
            }

            if (providerPlugins == null)
            {
                return null;
            }

            // Find provider in our plugins for this type id
            foreach (var p in providerPlugins)
            {
                var provider = p.GetProvider(taskTypeId);
                if (provider != null)
                {
                    return provider;
                }
            }

            throw new ArgumentException("Deploy Task Type Unknown:" + (taskTypeId ?? "<none>"));
        }
    }
}
