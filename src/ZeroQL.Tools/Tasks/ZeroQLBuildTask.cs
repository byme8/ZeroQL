using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ZeroQL.CLI;

namespace ZeroQL.Tasks;

public class ZeroQLBuildTask : Task
{
    [Required]
    public ITaskItem? ConfigFile { get; set; } 
    
    [Output]
    public string OutputPath { get; set; }
    
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
            : config.Output;;
        
        OutputPath = output;
        
        return true;
    }
}