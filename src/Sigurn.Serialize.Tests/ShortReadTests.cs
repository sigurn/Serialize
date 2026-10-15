using Xunit;

namespace Sigurn.Serialize.Tests;

public class ShortReadTests
{
    // A stream may hand back fewer bytes than were asked for, as a decompressing one does at the edge of its buffer.
    private sealed class OneByteAtATimeStream(byte[] data) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
    }

    private static async Task<byte[]> WrittenAsync<T>(T value, SerializationContext context)
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        return stream.ToArray();
    }

    [Fact]
    public async Task ANumber_IsReadFromAStreamThatGivesOneByteAtATime()
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = ByteOrder.BigEndian };
        using var stream = new OneByteAtATimeStream(await WrittenAsync(-1234.5678, context));

        Assert.Equal(-1234.5678, await Serializer.FromStreamAsync<double>(stream, context, CancellationToken.None));
    }

    [Fact]
    public async Task AnArrayOfAbsentAndPresentValues_IsReadFromAStreamThatGivesOneByteAtATime()
    {
        var context = new SerializationContext { AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian };
        int?[] values = [1, null, -3];
        using var stream = new OneByteAtATimeStream(await WrittenAsync(values, context));

        Assert.Equal(values, await Serializer.FromStreamAsync<int?[]>(stream, context, CancellationToken.None));
    }

    [Fact]
    public async Task AStringAndAGuid_AreReadFromAStreamThatGivesOneByteAtATime()
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = ByteOrder.BigEndian };
        var guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        using var stream = new OneByteAtATimeStream([.. await WrittenAsync("Привет", context), .. await WrittenAsync(guid, context)]);

        Assert.Equal("Привет", await Serializer.FromStreamAsync<string>(stream, context, CancellationToken.None));
        Assert.Equal(guid, await Serializer.FromStreamAsync<Guid>(stream, context, CancellationToken.None));
    }

    [Fact]
    public async Task ANumberCutShort_IsRefused()
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = ByteOrder.BigEndian };
        using var stream = new OneByteAtATimeStream((await WrittenAsync(42L, context))[..5]);

        await Assert.ThrowsAsync<SerializationException>(() => Serializer.FromStreamAsync<long>(stream, context, CancellationToken.None).AsTask());
    }
}
