namespace TranscriptService.Domain.ValueObjects;

/// <summary>
/// Describes the format of raw PCM audio - the metadata needed to build a valid WAV header.
/// </summary>
public sealed record PcmFormat(
    int SampleRateHz,
    short Channels,
    short BitsPerSample)
{
    public short BlockAlign => (short)(Channels * (BitsPerSample / 8));
    public int ByteRate => SampleRateHz * BlockAlign;
}
