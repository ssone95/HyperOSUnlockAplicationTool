using HOSUnlock.Constants;
using HOSUnlock.Services;
using System.Reflection;

namespace HOSUnlock.Configuration;

public class AppConfiguration
{
    public TokenInfo[] Tokens { get; set; } = [];
    public int[] TokenShifts { get; set; } = [];

    public bool AutoRunOnStart { get; set; } = false;

    public static AppConfiguration Instance { get; set; } = null!;

    public static AppConfiguration LoadDefault()
    {
        return new AppConfiguration
        {
            Tokens =
            [
                new TokenInfo
                {
                    Token = TokenConstants.DefaultTokenValue,
                    Index = 1
                }
            ]
        };
    }

    public bool IsConfigurationValid()
    {
        return
            Tokens.Length >= 1
            && !Tokens.Any(t => string.IsNullOrWhiteSpace(t.Token) || t.Token == TokenConstants.DefaultTokenValue || t.Index < 1)
            && !Tokens.Any(t => Tokens.Count(x => x.Index == t.Index) > 1)
            && TokenShifts.Length >= 1
            && !TokenShifts.Any(ts => TokenShifts.Count(x => x == ts) > 1);
    }

    public static async Task Load(string configPath = "")
    {
        var config = LoadDefault();
        var configFileName = "appsettings.json";

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
        catch (InvalidDataException)
        {
            // Invalid config - use default config
            // Don't show MessageBox here as UI loop hasn't started yet
            Logger.LogWarning($"Configuration file '{configFileName}' is invalid or missing required fields. Using default configuration.");
        }
        catch (Exception)
        {
            // Other errors - use default config
            // Don't show MessageBox here as UI loop hasn't started yet
        }

        Instance = config;
    }

    public static async Task<AppConfiguration> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Configuration file not found.", filePath);

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var configFromFile = System.Text.Json.JsonSerializer.Deserialize<AppConfiguration>(json);
        if (configFromFile == null || !configFromFile.IsConfigurationValid())
        {
            throw new InvalidDataException("Configuration file is invalid or missing required fields.");
        }

        return configFromFile;
    }
}

public sealed record TokenInfo : IComparable<TokenInfo>, IEquatable<TokenInfo>
{
    public string Token { get; set; } = TokenConstants.DefaultTokenValue;
    public int Index { get; set; } = 0;

    public override int GetHashCode()
    {
        return Token.Length + Index;
    }

    public bool Equals(TokenInfo? other)
    {
        if (other == null)
            return false;

        return string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase) && Index == other.Index;
    }

    public int CompareTo(TokenInfo? other)
    {
        if (other == null)
            return 1;

        var indexComparison = Index.CompareTo(other.Index);
        if (indexComparison != 0)
            return indexComparison;

        return string.Compare(Token, other.Token, StringComparison.OrdinalIgnoreCase);
    }
}