using Microsoft.Extensions.Logging;
using System.Buffers;
using TranscriptService.Application.Interfaces;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Application.Services
{
    public class WavConverter(
    IRecordingSessionServices recordingSessionServices,
    ILogger<WavConverter> logger) : IWavConverter
    {
        private static readonly PcmFormat DefaultFormat = new(
            SampleRateHz: 16_000,
            Channels: 1,
            BitsPerSample: 16);

        public async Task<Stream> ConvertAsync(
            RecordingId recordingId,
            ChunkId chunkId,
            IMemoryOwner<byte> audioBytes,
            CancellationToken cancellationToken)
        {
            var recordingMetaData = await recordingSessionServices.GetRecordingMetadataAsync(recordingId, cancellationToken);

            var format = recordingMetaData is not null
                ? new PcmFormat(
                    SampleRateHz: recordingMetaData.SampleRate,
                    Channels: (short)recordingMetaData.ChannelCount,
                    BitsPerSample: (short)recordingMetaData.BitsPerSample)
                : DefaultFormat;

            if (recordingMetaData is null)
            {
                logger.LogWarning(
                    "No recording metadata found for RecordingId: {RecordingId}, falling back to default PCM format",
                    recordingId);
            }

            var stream = await WavStream.CreateWavStreamAsync(audioBytes, format);

            logger.LogInformation("Completed WAV conversion for ChunkId: {ChunkId}", chunkId);

            return stream;
        }
    }
}
