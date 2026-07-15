using RecordingGrpcService.Grpc.Protos;
using Riok.Mapperly.Abstractions;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Infrastructure.Mappers;

[Mapper]
public partial class RecordingMapper : MapperBase
{
    // ---- RecordingMetadata (matches by property name/type, no custom code needed) ----
    public partial RecordingMetadata ToDomain(RecordingMetadataDto dto);
    public partial RecordingMetadataDto ToDto(RecordingMetadata metadata);

    // ---- RecordingId (string <-> value object) ----
    private RecordingId MapRecordingId(string id) => RecordingId.Of(ParseGuid(id));
    public string MapRecordingId(RecordingId id) => id.Value.ToString();
}

