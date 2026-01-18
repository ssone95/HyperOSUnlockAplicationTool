namespace HOSUnlock.Configuration;

/// <summary>
/// Parses command-line arguments into CommandLineOptions.
/// </summary>
public static class CommandLineParser
{
    private static readonly HashSet<string> KnownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--headless",
        "--auto-run",
        "--fixed-retry-wait",
        "--help",
        "-h"
    };

    private static readonly HashSet<string> KnownOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "--max-retries",
        "--max-api-retries",
        "--api-retry-wait"
    };

    /// <summary>
    /// Parses command-line arguments and returns the options.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="options">Parsed options if successful.</param>
    /// <param name="errorMessage">Error message if parsing failed.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string[] args, out CommandLineOptions options, out string? errorMessage)
    {
        options = new CommandLineOptions();
        errorMessage = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // Check for help
            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = GetHelpText();
                return false;
            }

            // Boolean flags
            if (arg.Equals("--headless", StringComparison.OrdinalIgnoreCase))
            {
                options.HeadlessMode = true;
                continue;
            }

            if (arg.Equals("--auto-run", StringComparison.OrdinalIgnoreCase))
            {
                options.AutoRunOnStart = true;
                continue;
            }

            if (arg.Equals("--fixed-retry-wait", StringComparison.OrdinalIgnoreCase))
            {
                options.MultiplyApiRetryWaitTimeByAttempt = false;
                continue;
            }

            // Options with values
            if (arg.Equals("--max-retries", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryGetIntValue(args, ref i, "--max-retries",
                    AppConfiguration.MinAutoRetries, AppConfiguration.MaxAutoRetriesLimit,
                    out var value, out errorMessage))
                {
                    return false;
                }
                options.MaxAutoRetries = value;
                continue;
            }

            if (arg.Equals("--max-api-retries", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryGetIntValue(args, ref i, "--max-api-retries",
                    AppConfiguration.MinApiRetries, AppConfiguration.MaxApiRetriesLimit,
                    out var value, out errorMessage))
                {
                    return false;
                }
                options.MaxApiRetries = value;
                continue;
            }

            if (arg.Equals("--api-retry-wait", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryGetIntValue(args, ref i, "--api-retry-wait",
                    AppConfiguration.MinApiRetryWaitTimeMs, AppConfiguration.MaxApiRetryWaitTimeMsLimit,
                    out var value, out errorMessage))
                {
                    return false;
                }
                options.ApiRetryWaitTimeMs = value;
                continue;
            }

            // Unknown argument
            if (arg.StartsWith('-'))
            {
                errorMessage = $"Unknown argument: {arg}\n\nUse --help to see available options.";
                return false;
            }
        }

        return true;
    }

    private static bool TryGetIntValue(string[] args, ref int index, string optionName,
        int min, int max, out int value, out string? errorMessage)
    {
        value = 0;
        errorMessage = null;

        if (index + 1 >= args.Length)
        {
            errorMessage = $"{optionName} requires a value.";
            return false;
        }

        index++;
        var valueStr = args[index];

        if (!int.TryParse(valueStr, out value))
        {
            errorMessage = $"{optionName} value must be an integer, got: {valueStr}";
            return false;
        }

        if (value < min || value > max)
        {
            errorMessage = $"{optionName} value must be between {min} and {max}, got: {value}";
            return false;
        }

        return true;
    }

    public static string GetHelpText() => """
        HOSUnlock - Xiaomi Bootloader Unlock Tool

        Usage: HOSUnlock [options]

        Options:
          --headless              Run in headless mode (no TUI)
          --auto-run              Automatically start monitoring without prompts
          --max-retries <n>       Max auto-retry attempts (1-365, default: 5)
          --max-api-retries <n>   Max API/NTP retry attempts (0-10, default: 3)
          --api-retry-wait <ms>   Base retry wait time in ms (1-1000, default: 100)
          --fixed-retry-wait      Use fixed wait time instead of multiplying by attempt
          --help, -h              Show this help message

        Examples:
          HOSUnlock --headless --auto-run
          HOSUnlock --max-retries 10 --auto-run
          HOSUnlock --headless --max-api-retries 5 --api-retry-wait 200

        Configuration file (appsettings.json) values are used as defaults.
        Command-line arguments override configuration file values.
        """;
}
