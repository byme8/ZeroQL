using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
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

        var schema = ZeroQLSchema.GetJsonSchema();
        var json = File.ReadAllText(configFile);
        var errors = schema.Validate(json);

        if (errors.Count > 0)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Config file is not valid.");
            stringBuilder.AppendLine("Errors:");
            foreach (var error in errors)
            {
                var humanReadableError = ZeroQLSchema.GetHumanReadableErrorMessage(error.Kind);
                stringBuilder.AppendLine(
                    $"    {configFile} [{error.LineNumber}:{error.LinePosition}]: {humanReadableError} at {error.Path}");
            }

            return new Error(stringBuilder.ToString());
        }

        var config = JsonConvert.DeserializeObject<ZeroQLFileConfig>(json);
        if (config is null)
        {
            return new Error("Config file is not valid. Check that file is valid.");
        }

        if (config.Output is null)
        {
            var configFileName = Path.GetFileName(configFile);
            config.Output = $"./obj/ZeroQL/{configFileName}.g.cs";
        }

        return config;
    }
}