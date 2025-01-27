using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.

namespace AspireRagDemo.API.Models;

public static class FileMetadataToQdrantConverter
{
    public static MapField<string, Value> ToQdrantFields(FileMetadata metadata)
    {
        var fields = new MapField<string, Value>();

        // String Values
        AddStringField(fields, "repository_organization", metadata.RepositoryOrganization);
        AddStringField(fields, "file_name", metadata.FileName);
        AddStringField(fields, "file_type", metadata.FileType);
        AddStringField(fields, "directory", metadata.Directory);
        AddStringField(fields, "file_path", metadata.FilePath);
        AddStringField(fields, "repository_name", metadata.RepositoryName);
        AddStringField(fields, "repository_url", metadata.RepositoryUrl);

        // Integer Values
        AddIntegerField(fields, "num_lines", metadata.NumLines);
        AddIntegerField(fields, "repository_total_files", metadata.RepositoryTotalFiles);
        AddIntegerField(fields, "size_bytes", metadata.SizeBytes);

        // Boolean Values
        AddBooleanField(fields, "is_empty", metadata.IsEmpty);
        AddBooleanField(fields, "has_shebang", metadata.HasShebang);

        // Struct Values (if needed)
        AddStructField(fields, "repository_file_types", metadata.RepositoryFileTypes);
        AddStructField(fields, "repository_directory_structure", metadata.RepositoryDirectoryStructure);

        return fields;
    }

    private static void AddStringField(MapField<string, Value> fields, string key, StringValue value)
    {
        if (value?.Value != null)
        {
            fields[key] = new Value { StringValue = value.Value };
        }
    }

    private static void AddIntegerField(MapField<string, Value> fields, string key, IntegerValue value)
    {
        if (value?.Value != null)
        {
            if (long.TryParse(value.Value, out long parsedValue))
            {
                fields[key] = new Value { IntegerValue = parsedValue };
            }
        }
    }

    private static void AddBooleanField(MapField<string, Value> fields, string key, BoolValue value)
    {
        if (value?.Value.HasValue == true)
        {
            fields[key] = new Value { BoolValue = value.Value.Value };
        }
    }

    private static void AddStructField(MapField<string, Value> fields, string key, StructValue value)
    {
        if (value?.Value != null)
        {
            fields[key] = new Value { StructValue = new Struct() };
        }
    }

    // Reverse Conversion (Qdrant Fields to FileMetadata)
    public static FileMetadata FromQdrantFields(MapField<string, Value> fields)
    {
        return new FileMetadata
        {
            RepositoryOrganization = GetStringValue(fields, "repository_organization"),
            FileName = GetStringValue(fields, "file_name"),
            NumLines = GetIntegerValue(fields, "num_lines"),
            FileType = GetStringValue(fields, "file_type"),
            IsEmpty = GetBoolValue(fields, "is_empty"),
            RepositoryName = GetStringValue(fields, "repository_name"),
            RepositoryTotalFiles = GetIntegerValue(fields, "repository_total_files"),
            HasShebang = GetBoolValue(fields, "has_shebang"),
            SizeBytes = GetIntegerValue(fields, "size_bytes"),
            Directory = GetStringValue(fields, "directory"),
            FilePath = GetStringValue(fields, "file_path"),
            RepositoryUrl = GetStringValue(fields, "repository_url")
        };
    }

    private static StringValue GetStringValue(MapField<string, Value> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.KindCase == Value.KindOneofCase.StringValue
            ? new StringValue { Value = value.StringValue }
            : null;
    }

    private static IntegerValue GetIntegerValue(MapField<string, Value> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.KindCase == Value.KindOneofCase.IntegerValue
            ? new IntegerValue { Value = value.IntegerValue.ToString() }
            : null;
    }

    private static BoolValue GetBoolValue(MapField<string, Value> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.KindCase == Value.KindOneofCase.BoolValue
            ? new BoolValue { Value = value.BoolValue }
            : null;
    }
}