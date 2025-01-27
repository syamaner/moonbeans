using System.Text.Json.Serialization;

namespace AspireRagDemo.API.Models;

public class FileMetadata
{
    [JsonPropertyName("repository_organization")]
    public StringValue? RepositoryOrganization { get; set; }

    [JsonPropertyName("file_name")]
    public StringValue? FileName { get; set; }

    [JsonPropertyName("num_lines")]
    public IntegerValue? NumLines { get; set; }

    [JsonPropertyName("file_type")]
    public StringValue? FileType { get; set; }

    [JsonPropertyName("is_empty")]
    public BoolValue? IsEmpty { get; set; }

    [JsonPropertyName("repository_name")]
    public StringValue? RepositoryName { get; set; }

    [JsonPropertyName("repository_total_files")]
    public IntegerValue? RepositoryTotalFiles { get; set; }

    [JsonPropertyName("has_shebang")]
    public BoolValue? HasShebang { get; set; }

    [JsonPropertyName("repository_file_types")]
    public StructValue? RepositoryFileTypes { get; set; }

    [JsonPropertyName("repository_directory_structure")]
    public StructValue? RepositoryDirectoryStructure { get; set; }

    [JsonPropertyName("size_bytes")]
    public IntegerValue? SizeBytes { get; set; }

    [JsonPropertyName("directory")]
    public StringValue? Directory { get; set; }

    [JsonPropertyName("file_path")]
    public StringValue? FilePath { get; set; }

    [JsonPropertyName("repository_url")]
    public StringValue? RepositoryUrl { get; set; }
}

public class StringValue
{
    [JsonPropertyName("stringValue")]
    public string? Value { get; set; }
}

public class IntegerValue
{
    [JsonPropertyName("integerValue")]
    public string? Value { get; set; }
}

public class BoolValue
{
    [JsonPropertyName("boolValue")]
    public bool? Value { get; set; }
}

public class StructValue
{
    [JsonPropertyName("structValue")]
    public object? Value { get; set; }
}