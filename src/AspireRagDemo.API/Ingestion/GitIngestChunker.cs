using LangChain.Splitters.Text;

namespace AspireRagDemo.API.Ingestion;

public class GitIngestChunker : IChunker
{
    private readonly Dictionary<string, TextSplitter> _splitters;

    private readonly CharacterTextSplitter _characterSplitter = 
        new CharacterTextSplitter("\n", 600, 50);

    public GitIngestChunker()
    {
        var headersToSplitOn = new[] { "#", "##", "###", "####", "#####", "######" };
        _splitters = new Dictionary<string, TextSplitter>
        {
            { ".md", new MarkdownHeaderTextSplitter(headersToSplitOn) },
            //add for yml
            { ".yml", _characterSplitter },
            { ".yaml", _characterSplitter }
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
            if(chunks.Any(x=>x.Length>600))
            {
                foreach (var chunk in chunks)
                {
                    if(chunk.Length>600)
                    {
                        var subChunks = _characterSplitter.SplitText(chunk);
                        fileChunks.Chunks.AddRange(subChunks);
                    }else{
                        fileChunks.Chunks.Add(chunk);
                    }
                }
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    fileChunks.Chunks.Add(chunk);
                }
            }

            yield return fileChunks;
        }

    }

    public bool CanChunk(DocumentType documentType)
    {
        return documentType == DocumentType.GitIngest;
    }
}