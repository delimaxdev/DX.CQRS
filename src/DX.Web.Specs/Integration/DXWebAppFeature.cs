using Autofac;
using DX.Testing;
using DX.Web;
using FluentAssertions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xbehave;
using Xunit.Abstractions;

namespace Integration
{
    internal static class HttpClientExtensions {
        public static async Task<string> GetSuccessful(this HttpClient client, string uri) {
            HttpResponseMessage message = await client.GetAsync(uri);
            message.IsSuccessStatusCode.Should().BeTrue();
            return await message.Content.ReadAsStringAsync();
        }
    }

    public class DXWebAppFeature : Feature {
        private ITestOutputHelper _output;

        public DXWebAppFeature(ITestOutputHelper output) {
            _output = output;
        }

        [Scenario]
        internal void ModuleIsolation(
            TestAppFactory appFactory,
            HttpClient client,
            string response
        ) {
            USING["a WebApplicationFactory"] = () => appFactory = new TestAppFactory();
            USING["a client"] = () => client = appFactory.CreateClient();
            When["issuing a request to first module"] = async () => response = await client.GetSuccessful("/mod1/test");
            THEN["the response is built using the Container of first module"] = () => response.Should().Be("Root:Module 1");
            When["issuing a request to second module"] = async () => response = await client.GetSuccessful("/mod2/test");
            THEN["the response is built using the Container of second module"] = () => response.Should().Be("Root:Module 2");
        }

        internal class TestAppFactory : WebApplicationFactory<TestApp> {
            protected override IWebHostBuilder CreateWebHostBuilder() {
                return WebHost
                    .CreateDefaultBuilder()
                    .ConfigureServices(services => services.AddSingleton<IStartup, TestApp>());
            }
        }

        internal class TestApp : DXWebApp {
            public TestApp(IConfiguration configuration) : base(configuration) { }

            protected override void RegisterModules(ModuleRegistrator modules) {
                modules.Register<Module1>("/mod1");
                modules.Register<Module2>("/mod2");
            }

            protected override void ConfigureGlobalContainer(ContainerBuilder rootContainer, IServiceProvider aspnetServices) {
                base.ConfigureGlobalContainer(rootContainer, aspnetServices);
                rootContainer.RegisterInstance(new RootService { Value = "Root" });
            }
        }



        internal class Module1 : DXModule {
            public Module1(IConfiguration configuration) : base(configuration) {
            }

            protected override void ConfigureServices(IServiceCollection services, IServiceProvider globalServices) {
                services.AddMvcCore(o => o.EnableEndpointRouting = false).AddApplicationPart(typeof(TestController).Assembly);
            }

            protected override void ConfigureContainer(ContainerBuilder builder) {
                base.ConfigureContainer(builder);
                builder.RegisterInstance(new SampleService { Value = "Module 1" });
            }

            protected override void Configure(IApplicationBuilder app) {
                app.UseMvc();
            }
        }

        internal class Module2 : DXModule {
            public Module2(IConfiguration configuration) : base(configuration) {
            }

            protected override void ConfigureServices(IServiceCollection services, IServiceProvider globalServices) {
                services.AddMvcCore(o => o.EnableEndpointRouting = false).AddApplicationPart(typeof(TestController).Assembly);

            }
            protected override void ConfigureContainer(ContainerBuilder builder) {
                base.ConfigureContainer(builder);
                builder.RegisterInstance(new SampleService { Value = "Module 2" });
            }

            protected override void Configure(IApplicationBuilder app) {
                app.UseMvc();
            }
        }

        public class SampleService {
            public string Value { get; set; } = "Default";
        }

        public class RootService {
            public string Value { get; set; } = "Default";
        }
    }

    [Route("[controller]")]
    [ApiController]
    public class TestController : ControllerBase {
        private readonly DXWebAppFeature.SampleService _service;
        private readonly DXWebAppFeature.RootService _rootService;

        public TestController(DXWebAppFeature.RootService rootService, DXWebAppFeature.SampleService service) {
            _service = service;
            _rootService = rootService;
        }

        [HttpGet]
        public string Get() {
            return $"{_rootService.Value}:{_service.Value}";
        }
    }
}
