using System;
using System.IO;
using System.Text.Json;
using ZeroQL.Core.Config;

namespace ZeroQL.Config;

public static class ZeroQLConfigReader
{
    public static Result<ZeroQLFileConfig> ReadConfig(string? configFile)
    {
        if (configFile is null)
        {
            return new Error("Config file is not specified. Use --config option to specify config file.");
        }

        if (!File.Exists(configFile))
        {
            return new Error($"Config file '{configFile}' does not exist. Check that file exist.");
        }

        var json = File.ReadAllText(configFile);

        ZeroQLFileConfig? config;
        try
        {
            config = JsonSerializer.Deserialize(json, ZeroQLJsonContext.Default.ZeroQLFileConfig);
        }
        catch (JsonException ex)
        {
            return new Error($"Config file is not valid JSON: {ex.Message}");
        }

        if (config is null)
        {
            return new Error("Config file is not valid. Check that file is valid.");
        }

        var validationError = ValidateConfig(config, configFile);
        if (validationError is not null)
        {
            return validationError;
        }

        if (config.Output is null)
        {
            var configFileName = Path.GetFileName(configFile);
            config.Output = $"./obj/ZeroQL/{configFileName}.g.cs";
        }

        return config;
    }

    private static Error? ValidateConfig(ZeroQLFileConfig config, string configFile)
    {
        if (string.IsNullOrWhiteSpace(config.GraphQL))
        {
            return new Error($"{configFile}: 'graphql' is required.");
        }

        if (string.IsNullOrWhiteSpace(config.Namespace))
        {
            return new Error($"{configFile}: 'namespace' is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ClientName))
        {
            return new Error($"{configFile}: 'clientName' is required.");
        }

        return null;
    }
}
