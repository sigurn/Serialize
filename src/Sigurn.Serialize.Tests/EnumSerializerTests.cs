namespace Sigurn.Serialize.Tests;

enum ByteEnum : byte
{
    Value1 = 1,
    Value2 = 178
}

enum SByteEnum : sbyte
{
    Value1 = -5,
    Value2 = -120
}

enum IntEnum : int
{
    Value1 = int.MinValue,
    Value2 = int.MaxValue
}

enum LongEnum : long
{
    Value1 = long.MinValue,
    Value2 = long.MaxValue
}

public class EnumSerializerTests
{
    [Fact]
    public async Task SerializeByteEnum()
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, ByteEnum.Value2, SerializationContext.Default, CancellationToken.None);
        byte[] data = stream.ToArray();
        Assert.Equal([178], data);
    }

    [Fact]
    public async Task DeserializeByteEnum()
    {
        using var stream = new MemoryStream([178]);
        var value = await Serializer.FromStreamAsync<ByteEnum>(stream, SerializationContext.Default, CancellationToken.None);
        Assert.Equal(ByteEnum.Value2, value);
    }

    [Fact]
    public async Task SerializeSByteEnum()
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, SByteEnum.Value2, SerializationContext.Default, CancellationToken.None);
        byte[] data = stream.ToArray();
        Assert.Equal([136], data);
    }

    [Fact]
    public async Task DeserializeSByteEnum()
    {
        using var stream = new MemoryStream([136]);
        var value = await Serializer.FromStreamAsync<SByteEnum>(stream, SerializationContext.Default, CancellationToken.None);
        Assert.Equal(SByteEnum.Value2, value);
    }

    [Fact]
    public async Task SerializeIntEnum()
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, IntEnum.Value2, SerializationContext.Default, CancellationToken.None);
        byte[] data = stream.ToArray();
        Assert.Equal([127, 255, 255, 255], data);
    }

    [Fact]
    public async Task DeserializeIntEnum()
    {
        using var stream = new MemoryStream([127, 255, 255, 255]);
        var value = await Serializer.FromStreamAsync<IntEnum>(stream, SerializationContext.Default, CancellationToken.None);
        Assert.Equal(IntEnum.Value2, value);
    }

    [Fact]
    public async Task SerializeLongEnum()
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, LongEnum.Value2, SerializationContext.Default, CancellationToken.None);
        byte[] data = stream.ToArray();
        Assert.Equal([127, 255, 255, 255, 255, 255, 255, 255], data);
    }

    [Fact]
    public async Task DeserializeLongEnum()
    {
        using var stream = new MemoryStream([127, 255, 255, 255, 255, 255, 255, 255]);
        var value = await Serializer.FromStreamAsync<LongEnum>(stream, SerializationContext.Default, CancellationToken.None);
        Assert.Equal(LongEnum.Value2, value);
    }

    [Fact]
    public async Task DeserializeUnexistingLongEnum()
    {
        using var stream = new MemoryStream([127, 0, 0, 0, 0, 0, 0, 0]);
        await Assert.ThrowsAsync<SerializationException>(async () => 
            await Serializer.FromStreamAsync<LongEnum>(stream, SerializationContext.Default, CancellationToken.None));
    }
}