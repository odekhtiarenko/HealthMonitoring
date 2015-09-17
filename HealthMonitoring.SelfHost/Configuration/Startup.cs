﻿using System;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using HealthMonitoring.Persistence;
using Microsoft.Owin.Host.HttpListener;
using Newtonsoft.Json.Converters;
using Owin;
using Swashbuckle.Application;

namespace HealthMonitoring.SelfHost.Configuration
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            ConfigureSerializers(config);
            ConfigureRoutes(config);
            ConfigureSwagger(config);
            ConfigureDependencies(config);
            appBuilder.UseWebApi(config);
        }

        private static void ConfigureSerializers(HttpConfiguration config)
        {
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
        }

        private static void ConfigureRoutes(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute("Swagger", "api", null, null, new RedirectHandler(SwaggerDocsConfig.DefaultRootUrlResolver, "swagger/ui/index"));
        }

        private static void ConfigureSwagger(HttpConfiguration config)
        {
            config
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Health Monitoring Service");
                    c.IgnoreObsoleteActions();
                    c.IgnoreObsoleteProperties();
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi(c => { c.DisableValidator(); });
        }

        private void ConfigureDependencies(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(Program).Assembly).Where(t => typeof(ApiController).IsAssignableFrom(t)).AsSelf();
            builder.RegisterAssemblyTypes(typeof(HealthMonitorRegistry).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(SqlConfigurationStore).Assembly).AsSelf().AsImplementedInterfaces().SingleInstance();

            builder.RegisterInstance<IHealthMonitorRegistry>(new HealthMonitorRegistry(MonitorDiscovery.DiscoverAllInCurrentFolder()));
            builder.Register(c => new HealthMonitor(c.Resolve<IEndpointRegistry>(), TimeSpan.FromSeconds(5))).SingleInstance();
            var container = builder.Build();
            container.Resolve<HealthMonitor>();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private Assembly[] GetIndirectDependencies()
        {
            return new[]
            {
                typeof (OwinHttpListener).Assembly
            };
        }
    }
}