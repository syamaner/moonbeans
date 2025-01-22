namespace AspireRagDemo.API;

using System.Text.Json.Serialization;

public class VectorQueryResult
{
    [JsonPropertyName("id")]
    public IdInfo Id { get; set; } = null!;

    [JsonPropertyName("payload")]
    public Payload Payload { get; set; } = null!;

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;
}

public class IdInfo
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = null!;
}

public class Payload
{
    [JsonPropertyName("page_content")]
    public StringValue PageContent { get; set; } = null!;

    [JsonPropertyName("metadata")]
    public StructValue Metadata { get; set; } = null!;
}

public class StringValue
{
    [JsonPropertyName("stringValue")]
    public string Value { get; set; } = null!;
}

public class StructValue
{
    [JsonPropertyName("fields")]
    public Fields Fields { get; set; } = null!;
}

public class Fields
{
    [JsonPropertyName("size_bytes")]
    public IntegerValue? SizeBytes { get; set; }

    [JsonPropertyName("file_level_metadata")]
    public StructValue? FileLevelMetadata { get; set; }

    [JsonPropertyName("file_path")]
    public StringValue? FilePath { get; set; }

    [JsonPropertyName("num_lines")]
    public IntegerValue? NumLines { get; set; }

    [JsonPropertyName("file_name")]
    public StringValue? FileName { get; set; }

    [JsonPropertyName("directory")]
    public StringValue? Directory { get; set; }

    [JsonPropertyName("file_type")]
    public StringValue? FileType { get; set; }

    [JsonPropertyName("is_empty")]
    public BoolValue? IsEmpty { get; set; }

    [JsonPropertyName("has_shebang")]
    public BoolValue? HasShebang { get; set; }

    [JsonPropertyName("repository")]
    public StructValue? Repository { get; set; }
}

public class IntegerValue
{
    [JsonPropertyName("integerValue")]
    public string Value { get; set; } = null!;
}

public class BoolValue
{
    [JsonPropertyName("boolValue")]
    public bool Value { get; set; }
}

public class ListValue
{
    [JsonPropertyName("values")]
    public List<Value> Values { get; set; } = new();
}

public class Value
{
    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }

    [JsonPropertyName("boolValue")]
    public bool? BoolValue { get; set; }

    [JsonPropertyName("integerValue")]
    public string? IntegerValue { get; set; }

    [JsonPropertyName("listValue")]
    public ListValue? ListValue { get; set; }
}

