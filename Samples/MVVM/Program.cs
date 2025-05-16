using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGameGum;
using System;
using System.Threading.Tasks;

namespace MonoGameAndGum
{
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;

        public Program()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        private async Task RunAsync()
        {
            try
            {
                using var game = _serviceProvider.GetRequiredService<Game1>();
                game.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Game failed: {ex.Message}");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Game1>();
            services.AddSingleton<GumService>();
        }
    }
}