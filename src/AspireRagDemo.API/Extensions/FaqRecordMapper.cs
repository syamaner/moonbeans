using AspireRagDemo.ServiceDefaults;
using Microsoft.Extensions.VectorData;
using Qdrant.Client.Grpc;

namespace AspireRagDemo.API.Models;

public class FaqRecordMapper : IVectorStoreRecordMapper<FaqRecord, PointStruct>
{
    public PointStruct MapFromDataToStorageModel(FaqRecord dataModel)
    {
        var pointStruct = new PointStruct
        {
            Id = new PointId { Uuid = dataModel.Id.ToString() },
            Vectors = new Vectors(),
            Payload =
            {
                { Constants.ConnectionStringNames.FaqVectorName, dataModel.Content },
                { Constants.ConnectionStringNames.MetadataPayloadFielname, new Value { StructValue = new Struct() } }
            },
        };
        if (dataModel.Metadata != null)
        {
            var qdrantFields = FileMetadataToQdrantConverter.ToQdrantFields(dataModel.Metadata);
            pointStruct.Payload[Constants.ConnectionStringNames.MetadataPayloadFielname].StructValue.Fields
                .MergeFrom(qdrantFields);
        }

        if (dataModel.Vector != null)
        {
            var namedVectors = new NamedVectors();
            namedVectors.Vectors.Add(Constants.ConnectionStringNames.FaqVectorName,
                dataModel.Vector.Value.ToArray());
            pointStruct.Vectors.Vectors_ = namedVectors;
        }

        return pointStruct;
    }

    public FaqRecord MapFromStorageToDataModel(PointStruct storageModel, StorageToDataModelMapperOptions options)
    {
        var faqRecord = new FaqRecord
        {
            Id = Guid.Parse(storageModel.Id.Uuid),
            Content = storageModel.Payload[Constants.ConnectionStringNames.FaqPayloadFieldName].StringValue,
            Metadata = FileMetadataToQdrantConverter.FromQdrantFields(storageModel.Payload[Constants.ConnectionStringNames.MetadataPayloadFielname].StructValue
                .Fields),
            Vector = storageModel.Vectors != null
                ? new ReadOnlyMemory<float>(storageModel.Vectors.Vectors_.Vectors[Constants.ConnectionStringNames.FaqVectorName].Data
                    .ToArray())
                : null
        };

        return faqRecord;
    }
}