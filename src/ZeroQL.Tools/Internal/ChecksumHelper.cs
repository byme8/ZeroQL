using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZeroQL.Bootstrap;

namespace ZeroQL.Internal;

public class ChecksumHelper
{
    public static string GenerateChecksumFromSchemaFile(string schemaFile, GraphQlGeneratorOptions options)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(schemaFile);
        var checksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty).ToLower();
        return AppendOptionsToChecksum(checksum, options);
    }
    
    public static string GenerateChecksumFromInlineSchema(string schema, GraphQlGeneratorOptions options)
    {
        var checksum = GetHashFromString(schema);
        return AppendOptionsToChecksum(checksum, options);
    }
    
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

    private static string AppendOptionsToChecksum(string checksumHash, GraphQlGeneratorOptions options)
    {
        var serializedOptions = JsonSerializer.Serialize(options);
        var optionsChecksumHash = GetHashFromString(serializedOptions);
        return GetHashFromString(checksumHash + optionsChecksumHash);
    }

    private static string GetHashFromString(string value)
    {
        using var md5 = MD5.Create();
        var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", String.Empty).ToLower();
        return hash;
    }
    
}