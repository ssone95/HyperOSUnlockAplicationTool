using HOSUnlock.Configuration;
using HOSUnlock.Services;
using HOSUnlock.Views;
using Terminal.Gui;

namespace HOSUnlock;

public sealed class App
{
    public async Task Run(CommandLineOptions options)
    {
        try
        {
            Logger.InitializeLogger("UI", logToConsoleToo: false);
            await AppConfiguration.LoadAsync().ConfigureAwait(false);

            // Apply command-line overrides after config is loaded
            AppConfiguration.Instance?.ApplyCommandLineOverrides(options);
        }
        catch (Exception ex)
        {
            // Log but continue - UI will handle showing the error
            Console.Error.WriteLine($"Configuration loading error: {ex.Message}");
        }

        Application.Init();

        try
        {
            Logger.LogInfo("TUI Application started.");

            // Initialize ClockProvider if configuration is valid
            if (AppConfiguration.Instance?.IsConfigurationValid() == true)
            {
                try
                {
                    await ClockProvider.InitializeAsync().ConfigureAwait(false);
                    Logger.LogInfo("ClockProvider initialized successfully.");
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to initialize ClockProvider.", ex);
                    // Continue anyway - MainView will show the error
                }
            }

            // Run the main view
            Application.Run(new MainView(), ex =>
            {
                Logger.LogError("Unhandled exception in application loop.", ex);
                return false; // Don't suppress the exception
            });

            Logger.LogInfo("Application exited normally.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal error occurred: {0}", ex, ex.Message);
            MessageBox.ErrorQuery("Fatal Error", $"A fatal error occurred:\n{ex.Message}", "OK");
        }
        finally
        {
            ClockProvider.DisposeInstance();
            Logger.DisposeLogger();
            Application.Shutdown();
        }
    }
}
