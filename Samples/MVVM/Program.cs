using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using MonoGameGum;
using System;
using System.Threading.Tasks;

namespace MonoGameAndGum
{
    /// <summary>
    /// The main program class responsible for bootstrapping the game application.
    /// </summary>
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Program"/> class, setting up dependency injection.
        /// </summary>
        public Program()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// The entry point of the application, creating and running the program asynchronously.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        /// <summary>
        /// Runs the game asynchronously, handling any exceptions.
        /// </summary>
        /// <returns>A task representing the asynchronous game execution.</returns>
        private async Task RunAsync()
        {
            try
            {
                // Resolve and run the game instance
                using var game = _serviceProvider.GetRequiredService<Game1>();
                game.Run();
            }
            catch (Exception ex)
            {
                // Log and rethrow any game execution errors
                Console.WriteLine($"Game failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Configures the dependency injection services for the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Register core services
            services.AddSingleton<Game1>();
            services.AddSingleton<GumService>();
        }
    }
}