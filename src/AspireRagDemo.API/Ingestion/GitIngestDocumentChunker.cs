using LangChain.Splitters.Text;

namespace AspireRagDemo.API.Ingestion;

public class GitIngestDocumentChunker : IDocumentChunker
{
    private readonly Dictionary<string, TextSplitter> _splitters;

    public GitIngestDocumentChunker()
    {
        var headersToSplitOn = new[] { "#", "##", "###", "####", "#####", "######" };
        _splitters = new Dictionary<string, TextSplitter>
        {
            { "md", new MarkdownHeaderTextSplitter(headersToSplitOn) },
            { "txt", new RecursiveCharacterTextSplitter() },
            //add for yml
            { "yml", new RecursiveCharacterTextSplitter() },
            { "yaml", new RecursiveCharacterTextSplitter() }
        };
    }

    public async IAsyncEnumerable<FileChunks> GetChunks(string gitIngestFilePath)
    {
        var content = await File2.ReadAllTextAsync(gitIngestFilePath);
        var files = GitIngestParser.ParseContent(content);
        
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.Key);
            if (!_splitters.TryGetValue(extension, value: out var splitter)) continue;
            var fileChunks = new FileChunks(file.Key, []);

            var chunks = splitter.SplitText(file.Value);
            foreach (var chunk in chunks)
            {
                fileChunks.Chunks.Add(chunk);
            }

            yield return fileChunks;
        }

    }

    public bool CanChunk(DocumentType documentType)
    {
        return documentType == DocumentType.GitIngest;
    }
}