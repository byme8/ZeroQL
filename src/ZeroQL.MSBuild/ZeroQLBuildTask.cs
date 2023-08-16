using Microsoft.Build.Framework;
using ZeroQL.Core.Config;
using Task = Microsoft.Build.Utilities.Task;

namespace ZeroQL.Tasks;

public class ZeroQLBuildTask : Task
{
    [Required] public ITaskItem? ConfigFile { get; set; }

    [Output] public string CommandToExecute { get; set; }
    [Output] public string FileToIncludeInProject { get; set; }
    [Output] public string OutputFile { get; set; }

    public override bool Execute()
    {
        var configFile = ConfigFile?.ItemSpec ?? throw new ArgumentNullException(nameof(ConfigFile));
        var (config, error) = ZeroQLConfigReader.ReadConfig(configFile)
            .Unwrap();

        if (error)
        {
            Log.LogError("Failed to read config file: {0}", configFile);
            Log.LogError(error.Message);
            return false;
        }

        var output = string.IsNullOrEmpty(config.Output)
            ? configFile
            : config.Output!;

        var commandOutput = output;
        if (!commandOutput.Contains("./obj/"))
        {
            commandOutput = string.Empty;
        }

        FileToIncludeInProject = output.Contains("./obj/")
            ? output
            : string.Empty;

        CommandToExecute = string.IsNullOrEmpty(commandOutput)
            ? $"generate --config {configFile}"
            : $"generate --config {configFile} --output {commandOutput}";

        OutputFile = output;

        return true;
    }
}