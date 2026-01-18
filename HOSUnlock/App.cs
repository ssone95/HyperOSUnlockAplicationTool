using HOSUnlock.Configuration;
using HOSUnlock.Services;
using HOSUnlock.Views;
using Terminal.Gui;

namespace HOSUnlock;

public static class App
{
    public static async Task Run(CommandLineOptions options)
    {
        try
        {
            Logger.InitializeLogger("UI", logToConsoleToo: false);
            await AppConfiguration.LoadAsync().ConfigureAwait(false);

            AppConfiguration.Instance?.ApplyCommandLineOverrides(options);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Configuration loading error: {ex.Message}");
        }

        Application.Init();

        try
        {
            Logger.LogInfo("TUI Application started.");

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
                }
            }

            Application.Run(new MainView(), ex =>
            {
                Logger.LogError("Unhandled exception in application loop.", ex);
                return false;
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
