using System.Diagnostics.Metrics;

namespace AspireRagDemo.ServiceDefaults.Metrics;

public class AspireRagDemoIngestionMetrics
{
    public const string MeterName="aspire_rag_demo.ingestion";
    private readonly Meter _meter;
    private readonly Counter<int> _documentsProcessed;
    private readonly Counter<int> _chunksProcessed;
    private readonly Histogram<double> _chunkingTime;
    private readonly Histogram<double> _embeddingTime;
    private readonly Histogram<double> _documentIngestionTime;

    public AspireRagDemoIngestionMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _chunkingTime = _meter.CreateHistogram<double>("aspire_rag_demo.ingestion.chunking_time");
        _embeddingTime = _meter.CreateHistogram<double>("aspire_rag_demo.ingestion.embedding_time");
        _documentIngestionTime = _meter.CreateHistogram<double>("aspire_rag_demo.ingestion.document_ingestion_time");
        _documentsProcessed = _meter.CreateCounter<int>("aspire_rag_demo.ingestion.documents_processed");
        _chunksProcessed = _meter.CreateCounter<int>("aspire_rag_demo.ingestion.chunks_processed");
    }

    public void RecordChunkingTime(double chunkingTime, params KeyValuePair<string, object?>[] tags)
    {
        if (chunkingTime != 0)
            _chunkingTime.Record(chunkingTime, tags);
    }

    public void RecordEmbeddingTime(double embeddingTime, params KeyValuePair<string, object?>[] tags)
    {
        if (embeddingTime != 0)
            _embeddingTime.Record(embeddingTime, tags);
    }

    public void RecordIngestionTime(double documentIngestionTime, params KeyValuePair<string, object?>[] tags)
    {
        if (documentIngestionTime != 0)
            _documentIngestionTime.Record(documentIngestionTime, tags);
    }


    public void RecordProcessedDocumentCount(int documentCount, params KeyValuePair<string, object?>[] tags)
    {
        _documentsProcessed.Add(documentCount, tags);
    }

    public void RecordProcessedChunkCount(int chunkCount, params KeyValuePair<string, object?>[] tags)
    {
        _chunksProcessed.Add(chunkCount, tags);
    }
}