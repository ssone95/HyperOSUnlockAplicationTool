using HOSUnlock.Configuration;
using HOSUnlock.Services;
using HOSUnlock.Views;
using Terminal.Gui;

namespace HOSUnlock;

public class App
{
    public async Task Run(string[] args)
    {
        try
        {
            Logger.InitializeLogger("UI", logToConsoleToo: false);
            await AppConfiguration.LoadAsync().ConfigureAwait(false);
        }
        catch
        {
            // Configuration loading errors will be handled after UI init
        }

        Application.Init();

        try
        {
            Logger.LogInfo("Application started.");

            if (AppConfiguration.Instance is null || !AppConfiguration.Instance.IsConfigurationValid())
            {
                throw new InvalidOperationException(
                    "Application configuration is invalid. Please check whether the appsettings.json file is valid and present in the same directory as the main program.");
            }

            if (args.Any(y => string.Equals(y, "--auto-run", StringComparison.OrdinalIgnoreCase)))
            {
                AppConfiguration.Instance.AutoRunOnStart = true;
            }

            await ClockProvider.InitializeAsync().ConfigureAwait(false);

            Application.Run(new MainView(), ex =>
            {
                Logger.LogError("Unhandled exception in application loop.", ex);
                return false;
            });

            ClockProvider.DisposeInstance();
            Logger.LogInfo("Application exited.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal error occurred: {0}", ex, ex.Message);
            MessageBox.ErrorQuery("Fatal Error", $"A fatal error occurred:\n{ex.Message}", "OK");
        }
        finally
        {
            Logger.DisposeLogger();
            Application.Shutdown();
        }
    }
}
