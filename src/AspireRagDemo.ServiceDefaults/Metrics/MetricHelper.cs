using System.Diagnostics;

namespace AspireRagDemo.ServiceDefaults.Metrics;

public class MetricHelper: IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly AspireRagDemoIngestionMetrics _metrics;
    private readonly MetricNames _metricName;
    private readonly KeyValuePair<string, object?>[] _tags;
    // constructor
    public MetricHelper(AspireRagDemoIngestionMetrics metrics, MetricNames metricName, params KeyValuePair<string, object?>[] tags)
    {
        _metrics = metrics;
        _metricName = metricName;
        _tags = tags;
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        switch (_metricName)
        {
            case MetricNames.Chunking:
                _metrics.RecordChunkingTime(_stopwatch.Elapsed.TotalMilliseconds, _tags);
                break;
            case MetricNames.Embedding:
                _metrics.RecordEmbeddingTime(_stopwatch.Elapsed.TotalMilliseconds, _tags);
                break;
            case MetricNames.DocumentIngestion:
                _metrics.RecordIngestionTime(_stopwatch.Elapsed.TotalMilliseconds, _tags);
                break;
            default:
                break;
        }
    }
    
}