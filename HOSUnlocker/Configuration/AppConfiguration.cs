using HOSUnlock.Constants;
using HOSUnlock.Services;
using System.Reflection;
using System.Text.Json.Serialization;

namespace HOSUnlock.Configuration;

public sealed class AppConfiguration
{
    // Validation constants
    public const int MinAutoRetries = 1;
    public const int MaxAutoRetriesLimit = 365;
    public const int DefaultMaxAutoRetries = 5;

    public const int MinApiRetries = 0;
    public const int MaxApiRetriesLimit = 10;
    public const int DefaultMaxApiRetries = 3;

    public const int MinApiRetryWaitTimeMs = 1;
    public const int MaxApiRetryWaitTimeMsLimit = 1000;
    public const int DefaultApiRetryWaitTimeMs = 100;

    public required TokenInfo[] Tokens { get; init; }
    public required int[] TokenShifts { get; init; }

    [JsonIgnore]
    public bool AutoRunOnStart { get; set; }

    [JsonIgnore]
    public bool HeadlessMode { get; set; }

    // These are settable to allow command-line overrides
    public int MaxAutoRetries { get; set; } = DefaultMaxAutoRetries;

    public int MaxApiRetries { get; set; } = DefaultMaxApiRetries;

    public int ApiRetryWaitTimeMs { get; set; } = DefaultApiRetryWaitTimeMs;

    public bool MultiplyApiRetryWaitTimeByAttempt { get; set; } = true;

    public static AppConfiguration? Instance { get; private set; }

    public bool IsConfigurationValid()
    {
        if (Tokens.Length < 1 || TokenShifts.Length < 1)
            return false;

        // Check for invalid tokens
        if (Tokens.Any(t => string.IsNullOrWhiteSpace(t.Token) ||
                            t.Token == TokenConstants.DefaultTokenValue ||
                            t.Index < 1))
            return false;

        // Check for duplicate token indices
        if (Tokens.DistinctBy(t => t.Index).Count() != Tokens.Length)
            return false;

        // Check for duplicate shift values
        if (TokenShifts.Distinct().Count() != TokenShifts.Length)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the validated MaxAutoRetries value, clamped to valid range.
    /// </summary>
    public int GetValidatedMaxAutoRetries()
        => Math.Clamp(MaxAutoRetries, MinAutoRetries, MaxAutoRetriesLimit);

    /// <summary>
    /// Gets the validated MaxApiRetries value, clamped to valid range.
    /// </summary>
    public int GetValidatedMaxApiRetries()
        => Math.Clamp(MaxApiRetries, MinApiRetries, MaxApiRetriesLimit);

    /// <summary>
    /// Gets the validated ApiRetryWaitTimeMs value, clamped to valid range.
    /// </summary>
    public int GetValidatedApiRetryWaitTimeMs()
        => Math.Clamp(ApiRetryWaitTimeMs, MinApiRetryWaitTimeMs, MaxApiRetryWaitTimeMsLimit);

    /// <summary>
    /// Applies command-line overrides to this configuration.
    /// </summary>
    public void ApplyCommandLineOverrides(CommandLineOptions options)
    {
        if (options.HeadlessMode.HasValue)
            HeadlessMode = options.HeadlessMode.Value;

        if (options.AutoRunOnStart.HasValue)
            AutoRunOnStart = options.AutoRunOnStart.Value;

        if (options.MaxAutoRetries.HasValue)
            MaxAutoRetries = options.MaxAutoRetries.Value;

        if (options.MaxApiRetries.HasValue)
            MaxApiRetries = options.MaxApiRetries.Value;

        if (options.ApiRetryWaitTimeMs.HasValue)
            ApiRetryWaitTimeMs = options.ApiRetryWaitTimeMs.Value;

        if (options.MultiplyApiRetryWaitTimeByAttempt.HasValue)
            MultiplyApiRetryWaitTimeByAttempt = options.MultiplyApiRetryWaitTimeByAttempt.Value;
    }

    public static async Task LoadAsync(string configPath = "")
    {
        var configFileName = "appsettings.json";
        AppConfiguration? config = null;

        try
        {
            var workingDir = string.IsNullOrEmpty(configPath)
                ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                : configPath;

            if (string.IsNullOrEmpty(workingDir))
                throw new DirectoryNotFoundException("Working directory could not be determined.");

            var configFilePath = Path.Combine(workingDir, configFileName);
            config = await LoadFromFileAsync(configFilePath).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            Logger.LogWarning($"Configuration file '{configFileName}' not found. Using default configuration.");
        }
        catch (InvalidDataException ex)
        {
            Logger.LogWarning($"Configuration file '{configFileName}' is invalid: {ex.Message}. Using default configuration.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error loading configuration: {ex.Message}. Using default configuration.");
        }

        Instance = config ?? LoadDefault();
    }

    public static AppConfiguration LoadDefault() => new()
    {
        Tokens = [new TokenInfo(TokenConstants.DefaultTokenValue, 1)],
        TokenShifts = [0]
    };

    public static async Task<AppConfiguration> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Configuration file not found.", filePath);

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var config = System.Text.Json.JsonSerializer.Deserialize<AppConfiguration>(json)
            ?? throw new InvalidDataException("Failed to deserialize configuration file.");

        if (!config.IsConfigurationValid())
            throw new InvalidDataException("Configuration file contains invalid or missing required fields.");

        return config;
    }

    // Keep backwards compatibility for existing code
    public static Task Load(string configPath = "") => LoadAsync(configPath);
}

/// <summary>
/// Represents authentication token information.
/// </summary>
public sealed record TokenInfo(
    [property: JsonPropertyName("Token")] string Token,
    [property: JsonPropertyName("Index")] int Index) : IComparable<TokenInfo>
{
    public int CompareTo(TokenInfo? other)
    {
        if (other is null)
            return 1;

        var indexComparison = Index.CompareTo(other.Index);
        if (indexComparison != 0)
            return indexComparison;

        return string.Compare(Token, other.Token, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(TokenInfo? other)
    {
        if (other is null)
            return false;

        return Index == other.Index
            && string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
        => HashCode.Combine(Index, StringComparer.OrdinalIgnoreCase.GetHashCode(Token));
}