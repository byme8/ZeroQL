using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZeroQL.Bootstrap;

namespace ZeroQL.Internal;

public static class ChecksumHelper
{
    /// <summary>
    /// Calculate a MD5 checksum from a schema string
    /// </summary>
    /// <param name="schemaFile">The schema file</param>
    /// <param name="options">The generator options</param>
    /// <returns></returns>
    public static string GenerateChecksumFromSchemaFile(string schemaFile, GraphQlGeneratorOptions options)
    {
        var text = File.ReadAllText(schemaFile); // Don't use File.ReadAllBytes(), since the file encoding headers (eg. UTF8 BOM) will become a part of the checksum
        var checksum = Checksum(text);

        return AppendOptionsToChecksum(checksum, options);
    }

    public static string Checksum(byte[] bytes)
    {
#if NETSTANDARD
        var hash = new MD5CryptoServiceProvider().ComputeHash(bytes);
#else
        var hash = MD5.HashData(bytes);
#endif
        var checksum = BitConverter.ToString(hash)
            .Replace("-", String.Empty)
            .ToLower();
        return checksum;
    }
    
    public static string Checksum(string text)
    {
        return Checksum(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Calculate a MD5 checksum from a schema string
    /// </summary>
    /// <param name="schema">The schema definition</param>
    /// <param name="options">The generator options</param>
    /// <returns></returns>
    public static string GenerateChecksumFromInlineSchema(string schema, GraphQlGeneratorOptions options)
    {
        var checksum = Checksum(schema);
        return AppendOptionsToChecksum(checksum, options);
    }

    /// <summary>
    /// Fetches the stored checksum from a previous generated source code file
    /// </summary>
    /// <param name="file">The file to fetch the checksum from</param>
    /// <returns></returns>
    public static string? ExtractChecksumFromSourceCode(string file)
    {
        var firstLine = File.ReadLines(file).FirstOrDefault();
        return firstLine?[3..];
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
        var optionsChecksumHash = Checksum(serializedOptions);

        // Also append the version of ZeroQL.CLI into the checksum
        // since updated tooling may affect the source code generated
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        return Checksum(checksumHash + optionsChecksumHash + version);
    }
}