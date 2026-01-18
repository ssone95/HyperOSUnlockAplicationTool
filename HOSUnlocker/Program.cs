using HOSUnlock.Configuration;

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

        // Determine if headless mode should be used (from args only at this point)
        var isHeadless = options.ShouldRunHeadless;

        if (isHeadless)
        {
            await HeadlessApp.Run(options);
        }
        else
        {
            await App.Run(options);
        }
    }
}