using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web1.Config
{
    public class ServiceFabricConfigSource : IConfigurationSource
    {
        public string PackageName { get; set; }

        public ServiceFabricConfigSource(string packageName)
        {
            PackageName = packageName;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ServiceFabricConfigurationProvider(PackageName);
        }
    }


}
