using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;

namespace Web1.Config
{
    public class ServiceFabricConfigurationProvider : ConfigurationProvider
    {
        private readonly string _packageName;
        private readonly CodePackageActivationContext _context;

        public ServiceFabricConfigurationProvider(string packageName)
        {
            _packageName = packageName;
            _context = FabricRuntime.GetActivationContext();
            _context.ConfigurationPackageModifiedEvent += (sender, e) =>
            {
                this.LoadPackage(e.NewPackage, reload: true);
                this.OnReload(); // Notify the change
            };
        }

        public override void Load()
        {
            var config = _context.GetConfigurationPackageObject(_packageName);
            LoadPackage(config);
        }

        private void LoadPackage(ConfigurationPackage config, bool reload = false)
        {
            if (reload)
            {
                Data.Clear();  // Rememove the old keys on re-load
            }
            foreach (var section in config.Settings.Sections)
            {
                foreach (var param in section.Parameters)
                {
                    Data[$"{section.Name}:{param.Name}"] = param.IsEncrypted ? param.DecryptValue().ToUnsecureString() : param.Value;
                }
            }
        }
    }
}
