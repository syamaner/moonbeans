using System.Text;

namespace AspireRagDemo.API.Ingestion;

public static class GitIngestFileSplitter
{
    private const string SeparatorLine = "=====================";
    private const string FilePrefix = "File:";

    public static Dictionary<string, string> ParseContent(string content)
    {
        var result = new Dictionary<string, string>();
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        
        string? currentFileName = null;
        var contentBuilder = new StringBuilder();
        var isCollectingContent = false;

        var skipNextSeperatorLine = false;
        foreach (var line in lines)
        {
            if (line.Trim().Contains(SeparatorLine))
            {
                if (currentFileName != null && isCollectingContent && !skipNextSeperatorLine)
                {
                    result[currentFileName] = contentBuilder.ToString().TrimEnd();
                    contentBuilder.Clear();
                    currentFileName = null;
                    isCollectingContent = false;
                    skipNextSeperatorLine = false;
                    continue;
                }
            }

            switch (isCollectingContent)
            {
                case false when line.StartsWith(FilePrefix):
                    currentFileName = line.Replace(FilePrefix,"").Trim();
                    isCollectingContent = true;
                    skipNextSeperatorLine = true;
                    continue;
                case true when currentFileName != null:
                {
                    skipNextSeperatorLine = false;
                    if (!line.Trim().Contains(SeparatorLine) && !string.IsNullOrWhiteSpace(line))
                    {
                        contentBuilder.AppendLine(line);
                    }

                    break;
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