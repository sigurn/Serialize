using Xunit;

namespace Sigurn.Serialize.Tests;

public class KeyValuePairSerializerTests
{
    public static IEnumerable<object[]> KeyValueData
    {
        get
        {
            yield return new object[]
            { 
                typeof(KeyValuePair<byte, byte>),
                new KeyValuePair<byte,byte>(0x00, 0xff),
                false,
                new byte[]{0x0, 0xff}
            };
            yield return new object[]
            { 
                typeof(KeyValuePair<byte?, byte?>),
                new KeyValuePair<byte?,byte?>(0x00, 0xff),
                true,
                new byte[]{0x01, 0x0, 0x01, 0xff}
            };
            yield return new object[]
            { 
                typeof(KeyValuePair<byte?, byte?>),
                new KeyValuePair<byte?,byte?>(0x00, 0xff),
                false,
                new byte[]{0x0, 0xff}
            };
            yield return new object[]
            { 
                typeof(KeyValuePair<short, short>),
                new KeyValuePair<short,short>(short.MinValue, short.MaxValue),
                false,
                new byte[]{0x00, 0x80, 0xff, 0x7f}
            };
        }
    }

    [Theory]
    [MemberData(nameof(KeyValueData))]
    public async Task SerializeKeyValuePair(Type type, object value, bool allowNullValues, byte[] expected)
    {
        var context = new SerializationContext(){ AllowNullValues = allowNullValues, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(KeyValueData))]
    public async Task DeserializeKeyValuePair(Type type, object expected, bool allowNullValues, byte[] data)
    {
        var context = new SerializationContext(){ AllowNullValues = allowNullValues, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream(data);
        var value = await Serializer.FromStreamAsync(stream, type, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }
}