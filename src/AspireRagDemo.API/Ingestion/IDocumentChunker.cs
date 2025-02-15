namespace AspireRagDemo.API.Ingestion;

public interface IDocumentChunker
{
    bool CanChunk(DocumentType documentType);
    IAsyncEnumerable<FileChunks> GetChunks(string gitIngestFilePath);
}