# Service Fabric / ASP.NET Core configuration sample

This sample shows how to expose Service Fabric configuration settings to ASP.NET Core, using the ASP.NET Core [configuration API](https://docs.microsoft.com/aspnet/core/fundamentals/configuration).

ASP.NET Core *configuration providers* to load application settings at run time. This sample implements a custom configuration provider for Service Fabric, so that an ASP.NET Core service can use the Service Fabric configuration model. 

> This article assumes that you are familiar with both Service Fabric and ASP.NET Core. For more information, see [Build a web service front end for your application using ASP.NET Core](https://docs.microsoft.com/azure/service-fabric/service-fabric-add-a-web-frontend).


To create a configuration provider, we'll derive from the `ConfigurationProvider` class. 

```csharp
public class ServiceFabricConfigurationProvider : ConfigurationProvider
```

 Service Fabric stores settings in [configuration packages](https://docs.microsoft.com/azure/service-fabric/service-fabric-application-model), which are accessible through the Service Fabric runtime. Overload the `ConfigurationProvider.Load` method to load settings from a configuration package:

```csharp
public override void Load()
{
    var config = _context.GetConfigurationPackageObject(_packageName);
    LoadPackage(config);
}

private void LoadPackage(ConfigurationPackage config, bool reload = false)
{
    if (reload)
    {
        Data.Clear();  // Clear the old keys on re-load
    }
    foreach (var section in config.Settings.Sections)
    {
        foreach (var param in section.Parameters)
        {
            Data[$"{section.Name}:{param.Name}"] = param.IsEncrypted ? param.DecryptValue().ToUnsecureString() : param.Value;
        }
    }
}
```    

> Service Fabric automatically loads the `Settings.xml` configuration file, but a configuration package can contain abritrary files in any format. If you use your own file, the Service Fabric API just gives you the file path. To support other file types, you would need to parse the file and turn it into a dictionary of key/value pairs. That's outside the scope of this sample.

It's possible to upgrade a service's configuration package without changing the code package. In that case, Service Fabric does not restart the service. Instead, the service receives a `ConfigurationPackageModifiedEvent` event to notify it that the package changed. We can hook into that event, by setting an event handler in the constructor of our provider: 

```csharp
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
```

> A service can have more than one configuration package, so we pass the package name in constructor.

The event handler calls our private `LoadPackage` method with the value `true` for the `reload` parameter. Then it calls the `OnReload` method, which is defined in the base class. This method notifies the ASP.NET Core configuration system that the values have changed.

The next thing that's needed is a *configuration source*, which acts as a factory for the configuration provider.

```csharp
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
```

Finally, we create an extension method that registers the configuration source with the configuration API:

```csharp
public static class ServiceFabricConfigExtensions
{
    public static IConfigurationBuilder AddServiceFabricConfig(this IConfigurationBuilder builder, string packageName)
    {
        return builder.Add(new ServiceFabricConfigSource(packageName));
    }
}
```

Call this extension method in the ASP.NET Core `Startup` method:

```csharp
public Startup(IHostingEnvironment env)
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        .AddServiceFabricConfig("Config") // Add Service Fabric configuration settings.
        .AddEnvironmentVariables();
    Configuration = builder.Build();
}
```

Now the ASP.NET Core service can access the Service Fabric configuration settings just like any other application settings. For example, you can use the options pattern to load settings into strongly typed objects:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<MyOptions>(Configuration);  // Strongly typed configuration object.
    services.AddMvc();
}
```

That's it!


