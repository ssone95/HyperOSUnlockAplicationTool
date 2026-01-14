using HOSUnlock.Constants;
using HOSUnlock.Services;
using System.Reflection;
using System.Text.Json.Serialization;

namespace HOSUnlock.Configuration;

public sealed class AppConfiguration
{
    public required TokenInfo[] Tokens { get; init; }
    public required int[] TokenShifts { get; init; }

    [JsonIgnore]
    public bool AutoRunOnStart { get; set; }

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