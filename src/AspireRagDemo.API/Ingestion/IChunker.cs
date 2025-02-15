namespace AspireRagDemo.API.Ingestion;

public interface IChunker
{
    bool CanChunk(DocumentType documentType);
    IAsyncEnumerable<FileChunks> GetChunks(string gitIngestFilePath);
}