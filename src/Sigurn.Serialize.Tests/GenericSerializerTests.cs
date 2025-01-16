using System.Text;

namespace Sigurn.Serialize.Tests;

class IntSerializer : ITypeSerializer<int>
{
    private readonly ByteOrder convByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

    public async Task<int> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        return await ReadTypeFromStream(stream, 4, 
            buf => BitConverter.ToInt32(convByteOrder != context.ByteOrder ? buf.Reverse().ToArray() : buf), cancellationToken);
    }

    public async Task ToStreamAsync(Stream stream, int value, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = BitConverter.GetBytes(value);
        if (convByteOrder != context.ByteOrder)
            buf = buf.Reverse().ToArray();
        await stream.WriteAsync(buf, cancellationToken);
    }

    private async Task<T?> ReadTypeFromStream<T>(Stream stream, int size, Func<byte[], T?> decode, CancellationToken cancellationToken)
    {
        byte[] buf = new byte[size];
        return await ReadTypeFromStream(stream, buf, decode, cancellationToken);
    }

    private async Task<T?> ReadTypeFromStream<T>(Stream stream, byte[] buf, Func<byte[], T?> decode, CancellationToken cancellationToken)
    {
        var len = await stream.ReadAsync(buf, cancellationToken);
        if (len != buf.Length)
            throw new SerializationException("Failed to read necessary amount of bytes from stream.");

        return decode(buf);
    }
}

public class GenericSerializerTests
{
    [Fact]
    public async Task SerializeIntValue()
    {
        var context = new SerializationContext
        { 
            Encoding = Encoding.ASCII,
            ByteOrder = ByteOrder.LittleEndian,
            AllowNullValues = false
        };

        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync<int>(stream, 0x11223344, context, CancellationToken.None);
        byte[] expected = [0x44, 0x33, 0x22, 0x11];
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task SerializeNullableIntValueWhenNullValuesAreNotAllowed()
    {
        var context = new SerializationContext
        { 
            Encoding = Encoding.ASCII,
            ByteOrder = ByteOrder.LittleEndian,
            AllowNullValues = false
        };

        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync<int?>(stream, 0x11223344, context, CancellationToken.None);
        byte[] expected = [0x44, 0x33, 0x22, 0x11];
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task SerializeNullableIntValueWhenNullValuesAreAllowed()
    {
        var context = new SerializationContext
        { 
            Encoding = Encoding.ASCII,
            ByteOrder = ByteOrder.LittleEndian,
            AllowNullValues = true
        };

        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync<int?>(stream, 0x11223344, context, CancellationToken.None);
        byte[] expected = [0x01, 0x44, 0x33, 0x22, 0x11];
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task SerializeNullableIntNullValueWhenNullValuesAreNotAllowed()
    {
        var context = new SerializationContext
        { 
            Encoding = Encoding.ASCII,
            ByteOrder = ByteOrder.LittleEndian,
            AllowNullValues = false
        };

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<SerializationException>(() => Serializer.ToStreamAsync<int?>(stream, null, context, CancellationToken.None));
    }

    [Fact]
    public async Task SerializeNullableIntNullValueWhenNullValuesAreAllowed()
    {
        var context = new SerializationContext
        { 
            Encoding = Encoding.ASCII,
            ByteOrder = ByteOrder.LittleEndian,
            AllowNullValues = true
        };

        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync<int?>(stream, null, context, CancellationToken.None);
        byte[] expected = [0x00];
        Assert.Equal(expected, stream.ToArray());
    }
}