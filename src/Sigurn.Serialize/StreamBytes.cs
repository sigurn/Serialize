using System.Buffers;

namespace Sigurn.Serialize;

/// <summary>
/// Reading and writing the raw bytes of a value: a read waits for every byte asked for, since a stream may hand back fewer than
/// were asked for at a time, and the scratch space a value passes through is borrowed from the shared pool, never set aside anew.
/// </summary>
static class StreamBytes
{
    private static readonly byte[] _absent = [0];
    private static readonly byte[] _present = [1];

    public static ValueTask WritePresenceAsync(Stream stream, bool present, CancellationToken cancellationToken) =>
        stream.WriteAsync(present ? _present : _absent, cancellationToken);

    public static async ValueTask<bool> ReadPresenceAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            await ReadExactlyAsync(stream, buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            return buffer[0] != 0;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        if (read != buffer.Length)
            throw new SerializationException("Failed to read neccessary data from stream");
    }
}
