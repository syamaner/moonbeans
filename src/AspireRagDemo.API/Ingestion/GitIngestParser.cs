using System.Text;

namespace AspireRagDemo.API.Ingestion;

public static class GitIngestParser
{
    private const string SeparatorLine = "================================================";
    private const string FilePrefix = "File: ";

    public static Dictionary<string, string> ParseContent(string content)
    {
        var result = new Dictionary<string, string>();
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        
        string? currentFileName = null;
        var contentBuilder = new StringBuilder();
        var isCollectingContent = false;

        foreach (var line in lines)
        {
            if (line.Trim() == SeparatorLine)
            {
                if (currentFileName != null && isCollectingContent)
                {
                    // Store the previous file's content
                    result[currentFileName] = contentBuilder.ToString().TrimEnd();
                    contentBuilder.Clear();
                    currentFileName = null;
                }
                isCollectingContent = !isCollectingContent;
                continue;
            }

            if (!isCollectingContent && line.StartsWith(FilePrefix))
            {
                currentFileName = line.Substring(FilePrefix.Length).Trim();
                continue;
            }

            if (isCollectingContent && currentFileName != null)
            {
                if (line.Trim() != SeparatorLine && string.IsNullOrWhiteSpace(line))
                {
                    contentBuilder.AppendLine(line);
                }
            }
        }

        // Don't forget to add the last file if there is one
        if (currentFileName != null && contentBuilder.Length > 0)
        {
            result[currentFileName] = contentBuilder.ToString().TrimEnd();
        }

        return result;
    } 
}