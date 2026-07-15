using TranscriptService.Domain.ValueObjects;
using System.Buffers;
using System.Buffers.Binary;

namespace TranscriptService.Application.Services;
public static class WavStream
{
    public static async Task<MemoryStream> CreateWavStreamAsync(IMemoryOwner<byte> pcmOwner, PcmFormat format)
    {
        using (pcmOwner) // we take ownership — caller hands it off, we dispose when done
        {
            return await Task.Run(() =>
            {
                ReadOnlySpan<byte> pcmBytes = pcmOwner.Memory.Span;

                short[] int16Samples = ConvertToInt16Samples(pcmBytes, format);

                short channels = format.Channels;
                short outputBitsPerSample = 16;
                int sampleRate = format.SampleRateHz;
                int byteRate = sampleRate * channels * outputBitsPerSample / 8;
                short blockAlign = (short)(channels * outputBitsPerSample / 8);

                int dataSize = int16Samples.Length * 2;

                var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + dataSize);
                    writer.Write("WAVE".ToCharArray());

                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((short)1); // PCM
                    writer.Write(channels);
                    writer.Write(sampleRate);
                    writer.Write(byteRate);
                    writer.Write(blockAlign);
                    writer.Write(outputBitsPerSample);

                    writer.Write("data".ToCharArray());
                    writer.Write(dataSize);

                    byte[] pcmOutBytes = new byte[dataSize];
                    Buffer.BlockCopy(int16Samples, 0, pcmOutBytes, 0, dataSize);
                    writer.Write(pcmOutBytes);
                }

                stream.Position = 0;
                return stream;
            });
        }
    }

    private static short[] ConvertToInt16Samples(ReadOnlySpan<byte> pcmBytes, PcmFormat format)
    {
        switch (format.BitsPerSample)
        {
            case 32 when IsFloatFormat(format):
                return ConvertFloat32ToInt16(pcmBytes);

            case 16:
                int sampleCount16 = pcmBytes.Length / 2;
                short[] samples16 = new short[sampleCount16];
                for (int i = 0; i < sampleCount16; i++)
                    samples16[i] = BinaryPrimitives.ReadInt16LittleEndian(pcmBytes.Slice(i * 2, 2));
                return samples16;

            case 24:
                return ConvertInt24ToInt16(pcmBytes);

            case 32:
                return ConvertInt32ToInt16(pcmBytes);

            case 8:
                return ConvertUInt8ToInt16(pcmBytes);

            default:
                throw new NotSupportedException(
                    $"Unsupported PCM bit depth: {format.BitsPerSample}");
        }
    }

    // NOTE: PcmFormat doesn't distinguish float vs int at 32-bit.
    // Add an `IsFloat` flag to PcmFormat if VoiceCaptureService ever emits 32-bit int PCM.
    private static bool IsFloatFormat(PcmFormat format) => true;

    private static short[] ConvertFloat32ToInt16(ReadOnlySpan<byte> float32Bytes)
    {
        int sampleCount = float32Bytes.Length / 4;
        short[] result = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = BinaryPrimitives.ReadSingleLittleEndian(float32Bytes.Slice(i * 4, 4));
            sample = Math.Clamp(sample, -1f, 1f);
            result[i] = (short)(sample * short.MaxValue);
        }
        return result;
    }

    private static short[] ConvertInt32ToInt16(ReadOnlySpan<byte> int32Bytes)
    {
        int sampleCount = int32Bytes.Length / 4;
        short[] result = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int sample = BinaryPrimitives.ReadInt32LittleEndian(int32Bytes.Slice(i * 4, 4));
            result[i] = (short)(sample >> 16);
        }
        return result;
    }

    private static short[] ConvertInt24ToInt16(ReadOnlySpan<byte> int24Bytes)
    {
        int sampleCount = int24Bytes.Length / 3;
        short[] result = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int offset = i * 3;
            int sample = (int24Bytes[offset + 2] << 16) | (int24Bytes[offset + 1] << 8) | int24Bytes[offset];
            if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000); // sign extend
            result[i] = (short)(sample >> 8);
        }
        return result;
    }

    private static short[] ConvertUInt8ToInt16(ReadOnlySpan<byte> uint8Bytes)
    {
        short[] result = new short[uint8Bytes.Length];
        for (int i = 0; i < uint8Bytes.Length; i++)
        {
            int sample = uint8Bytes[i] - 128;
            result[i] = (short)(sample << 8);
        }
        return result;
    }
}