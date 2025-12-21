using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace ZeroQL.Benchmark;

[MemoryDiagnoser]
public class CliBenchmark
{
    public const string SchemaFile = "../../TestApp/ZeroQL.TestApp/schema.graphql";
    public const string ConfigFile = "./benchmark.zeroql.json";
    public const string OutputFile = "./bin/CliGraphQL.g.cs";

    private string _cliPath = null!;
    private string _workingDirectory = null!;

    [GlobalSetup]
    public void Setup()
    {
        _workingDirectory = AppContext.BaseDirectory;

        var possiblePaths = new[]
        {
            Path.Combine(_workingDirectory, "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Release", "net10.0", "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Debug", "net10.0", "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Release", "net9.0", "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Debug", "net9.0", "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Release", "net8.0", "zeroql.dll"),
            Path.Combine(_workingDirectory, "..", "..", "..", "..", "ZeroQL.CLI", "bin", "Debug", "net8.0", "zeroql.dll"),
        };

        _cliPath = possiblePaths.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException(
                $"Could not find zeroql CLI. Searched paths:\n{string.Join("\n", possiblePaths.Select(p => $"  - {Path.GetFullPath(p)}"))}");

        Console.WriteLine($"Using CLI at: {_cliPath}");

        EnsureConfigExists();
        CleanupOutput();
        RunCliGenerate(force: true);
        CleanupOutput();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        CleanupOutput();
    }

    private void CleanupOutput()
    {
        var outputPath = Path.Combine(_workingDirectory, OutputFile);
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    private void EnsureConfigExists()
    {
        var configPath = Path.Combine(_workingDirectory, ConfigFile);
        if (File.Exists(configPath))
        {
            return;
        }

        var configContent = $$"""
            {
              "$schema": "https://raw.githubusercontent.com/byme8/ZeroQL/main/schema.verified.json",
              "graphql": "{{SchemaFile}}",
              "namespace": "GraphQL.CliBenchmark",
              "clientName": "CliBenchmarkClient",
              "output": "{{OutputFile}}"
            }
            """;
        File.WriteAllText(configPath, configContent);
    }

    [Benchmark]
    public int GenerateWithArgs()
    {
        return RunCliGenerate(force: true);
    }

    [Benchmark]
    public int GenerateWithConfig()
    {
        return RunCliGenerateWithConfig(force: true);
    }

    [Benchmark]
    public int GenerateWithChecksumSkip()
    {
        RunCliGenerate(force: true);
        return RunCliGenerate(force: false);
    }

    private int RunCliGenerate(bool force)
    {
        var schemaPath = Path.GetFullPath(Path.Combine(_workingDirectory, SchemaFile));
        var outputPath = Path.GetFullPath(Path.Combine(_workingDirectory, OutputFile));

        var args = $"generate -s \"{schemaPath}\" -o \"{outputPath}\" -n GraphQL.CliBenchmark -q CliBenchmarkClient";
        if (force)
        {
            args += " -f";
        }

        return RunCli(args);
    }

    private int RunCliGenerateWithConfig(bool force)
    {
        var configPath = Path.GetFullPath(Path.Combine(_workingDirectory, ConfigFile));

        var args = $"generate -c \"{configPath}\"";
        if (force)
        {
            args += " -f";
        }

        return RunCli(args);
    }

    private int RunCli(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {arguments}",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start CLI process");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            var output = process.StandardOutput.ReadToEnd();
            throw new InvalidOperationException($"CLI failed with exit code {process.ExitCode}.\nStdout: {output}\nStderr: {error}");
        }

        return process.ExitCode;
    }
}
