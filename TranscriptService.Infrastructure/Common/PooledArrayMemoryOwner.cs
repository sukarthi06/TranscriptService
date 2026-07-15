using System.Buffers;

namespace TranscriptService.Infrastructure.Common;

public sealed class PooledArrayMemoryOwner : IMemoryOwner<byte>
{
    private byte[]? _array;
    private readonly int _length;

    public PooledArrayMemoryOwner(byte[] array, int length)
    {
        _array = array;
        _length = length;
    }

    public Memory<byte> Memory =>
        _array != null
            ? _array.AsMemory(0, _length)
            : throw new ObjectDisposedException(nameof(PooledArrayMemoryOwner));

    public void Dispose()
    {
        if (_array != null)
        {
            ArrayPool<byte>.Shared.Return(_array);
            _array = null;
        }
    }
}
