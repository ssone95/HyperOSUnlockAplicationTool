using HOSUnlock.Configuration;
using HOSUnlock.Exceptions;

namespace HOSUnlock;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        // Parse command-line arguments
        if (!CommandLineParser.TryParse(args, out var options, out var errorMessage))
        {
            Console.WriteLine(errorMessage);
            Environment.Exit(errorMessage?.StartsWith("HOSUnlock") == true ? 0 : 1); // Exit 0 for help, 1 for errors
            return;
        }
        try
        {
            // Determine if headless mode should be used (from args only at this point)
            var isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            if (isInContainer)
            {
                Console.WriteLine("Detected running inside a container. Forcing headless mode...");
            }
            var isHeadless = isInContainer || options.ShouldRunHeadless;

            if (isHeadless)
            {
                await HeadlessApp.Run(options, isInContainer);
            }
            else
            {
                await App.Run(options);
            }
            Environment.Exit(0);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("HOSUnlocker operation was canceled.");
            Environment.Exit(1);
        }
        catch (MiException mex)
        {
            Console.WriteLine($"HOSUnlocker encountered a MiException: {mex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HOSUnlocker encountered an error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}