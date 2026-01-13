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
            Logger.InitializeLogger("UI", false);
            await LoadConfiguration().ConfigureAwait(false);
        }
        catch
        {
            // Ignore configuration loading errors here, they will be handled later
        }

        Application.Init();

        try
        {
            Logger.LogInfo("Application started.");

            if (AppConfiguration.Instance == null || !AppConfiguration.Instance.IsConfigurationValid())
            {
                throw new InvalidOperationException("Application configuration is invalid. Please check whether the appsettings.json file is valid and present in the same directory as the main program.");
            }

            if (args.Length > 0 && args.Any(y => string.Equals(y, "--auto-run", StringComparison.OrdinalIgnoreCase)))
            {
                AppConfiguration.Instance.AutoRunOnStart = true;
            }

            await ClockProvider.Initialize();

            Application.Run(new MainView(), (ex) =>
            {
                Logger.LogError("Unhandled exception in application loop.", ex);
                return false; // Let the application handle the exception
            });

            ClockProvider.DisposeInstance();
            Logger.LogInfo("Application exited.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal error occurred: {0}", ex, ex.Message);
            MessageBox.ErrorQuery("Fatal Error", $@"A fatal error occurred:
{ex.Message}", "OK");
        }
        finally
        {
            Logger.DisposeLogger();
            Application.Shutdown();
        }
    }


    private static async Task LoadConfiguration()
    {
        await AppConfiguration.Load();
    }
}
