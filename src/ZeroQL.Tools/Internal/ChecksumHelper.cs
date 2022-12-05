using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZeroQL.Bootstrap;

namespace ZeroQL.Internal;

public class ChecksumHelper
{
    /// <summary>
    /// Calculate a MD5 checksum from a schema string
    /// </summary>
    /// <param name="schemaFile">The schema file</param>
    /// <param name="options">The generator options</param>
    /// <returns></returns>
    public static string GenerateChecksumFromSchemaFile(string schemaFile, GraphQlGeneratorOptions options)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(schemaFile);
        var checksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty).ToLower();
        return AppendOptionsToChecksum(checksum, options);
    }
    
    /// <summary>
    /// Calculate a MD5 checksum from a schema string
    /// </summary>
    /// <param name="schema">The schema definition</param>
    /// <param name="options">The generator options</param>
    /// <returns></returns>
    public static string GenerateChecksumFromInlineSchema(string schema, GraphQlGeneratorOptions options)
    {
        var checksum = GetHashFromString(schema);
        return AppendOptionsToChecksum(checksum, options);
    }
    
    /// <summary>
    /// Fetches the stored checksum from a previous generated source code file
    /// </summary>
    /// <param name="file">The file to fetch the checksum from</param>
    /// <returns></returns>
    public static string? ExtractChecksumFromSourceCode(string file)
    {
        var regex = new Regex(@"<checksum>(?<checksum>\w+)<\/checksum>");
        foreach (var line in File.ReadLines(file))
        {
            if (!line.StartsWith("//"))
            {
                break;
            }

            var match = regex.Match(line);
            if (match.Success)
            {
                return match.Groups["checksum"].Value;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Append options affects the code to be outputted and needs to be a part of the calculated checksum
    /// </summary>
    /// <param name="checksumHash">The checksum based on graphql schema</param>
    /// <param name="options">The generator options</param>
    /// <returns></returns>
    private static string AppendOptionsToChecksum(string checksumHash, GraphQlGeneratorOptions options)
    {
        var serializedOptions = JsonSerializer.Serialize(options);
        var optionsChecksumHash = GetHashFromString(serializedOptions);
        
        // Also append the version of ZeroQL.CLI into the checksum
        // since updated tooling may affect the source code generated
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(); 
        return GetHashFromString(checksumHash + optionsChecksumHash + version);
    }

    /// <summary>
    /// Generates a MD5 checksum based on a input string
    /// </summary>
    /// <param name="value">The value to generate a checksum for</param>
    /// <returns>The MD5 checksum hash</returns>
    private static string GetHashFromString(string value)
    {
        using var md5 = MD5.Create();
        var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", String.Empty).ToLower();
        return hash;
    }
    
}