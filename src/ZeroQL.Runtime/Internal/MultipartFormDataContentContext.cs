using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZeroQL.Internal;

public class MultipartFormDataContentContext
{
    public List<UploadEntry> Uploads { get; } = new();
}

public class UploadEntry
{
    public int Index { get; set; }
    
    public string Path { get; set; }

    [JsonIgnore]
    public Func<Upload> Getter { get; set; }
}