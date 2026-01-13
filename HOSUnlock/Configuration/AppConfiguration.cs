using HOSUnlock.Constants;
using HOSUnlock.Services;
using System.Reflection;

namespace HOSUnlock.Configuration;

public class AppConfiguration
{
    public string Token1 { get; set; } = TokenConstants.DefaultTokenValue;
    public string Token2 { get; set; } = TokenConstants.DefaultTokenValue;
    public string Token3 { get; set; } = TokenConstants.DefaultTokenValue;
    public string Token4 { get; set; } = TokenConstants.DefaultTokenValue;

    public int Token1RequestShiftMilliseconds { get; set; } = 0;
    public int Token2RequestShiftMilliseconds { get; set; } = 0;
    public int Token3RequestShiftMilliseconds { get; set; } = 0;
    public int Token4RequestShiftMilliseconds { get; set; } = 0;

    public bool AutoRunOnStart { get; set; } = false;

    public static AppConfiguration Instance { get; set; } = null!;

    public static AppConfiguration LoadDefault()
    {
        return new AppConfiguration
        {
            Token1 = "DefaultTokenValue1",
            Token2 = "DefaultTokenValue2",
            Token3 = "DefaultTokenValue3",
            Token4 = "DefaultTokenValue4"
        };
    }

    public bool IsConfigurationValid()
    {
        return
            !string.IsNullOrEmpty(Token1)
            && !string.IsNullOrEmpty(Token2)
            && !string.IsNullOrEmpty(Token3)
            && !string.IsNullOrEmpty(Token4)
            && !string.Equals(Token1, TokenConstants.DefaultTokenValue)
            && !string.Equals(Token2, TokenConstants.DefaultTokenValue)
            && !string.Equals(Token3, TokenConstants.DefaultTokenValue)
            && !string.Equals(Token4, TokenConstants.DefaultTokenValue)
            && Token1RequestShiftMilliseconds > 0
            && Token2RequestShiftMilliseconds > 0
            && Token3RequestShiftMilliseconds > 0
            && Token4RequestShiftMilliseconds > 0;
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
