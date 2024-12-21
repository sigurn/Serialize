using Xunit;

namespace Sigurn.Serialize.Tests;

public class CollectionsSerializerTests
{
    public static IEnumerable<object?[]> ArrayData
    {
        get
        {
            yield return new object?[]
            { 
                typeof(byte[]),
                new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
                ByteOrder.LittleEndian,
                false,
                new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}
            };
            yield return new object?[]
            { 
                typeof(byte[]),
                new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
                ByteOrder.BigEndian,
                false,
                new byte[]{0x00, 0x00, 0x00, 0x10, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}
            };
            yield return new object?[]
            { 
                typeof(int[]),
                new int[] {1, 2, -3, -18},
                ByteOrder.LittleEndian,
                false,
                new byte[]{0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0xfd, 0xff, 0xff, 0xff, 0xee, 0xff, 0xff, 0xff}
            };
            yield return new object?[]
            { 
                typeof(int[]),
                new int[] {1,2,-3,-18},
                ByteOrder.BigEndian,
                false,
                new byte[]{0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xff, 0xee}
            };
            yield return new object?[]
            { 
                typeof(short[]),
                new short[] {1,2,-3,-18},
                ByteOrder.BigEndian,
                true,
                new byte[]{0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x02, 0xff, 0xfd, 0xff, 0xee}
            };
            yield return new object?[]
            { 
                typeof(long[]),
                null,
                ByteOrder.BigEndian,
                true,
                new byte[]{0x00}
            };
            yield return new object?[]
            { 
                typeof(short?[]),
                new short?[] {1,2,-3,-18, null},
                ByteOrder.BigEndian,
                true,
                new byte[]{0x01, 0x00, 0x00, 0x00, 0x05, 0x01, 0x00, 0x01, 0x01, 0x00, 0x02, 0x01, 0xff, 0xfd, 0x01, 0xff, 0xee, 0x00}
            };
        }
    }

    [Theory]
    [MemberData(nameof(ArrayData))]
    public async Task SerializeArrays(Type type, object value, ByteOrder byteOrder, bool allowNullValues, byte[] expected)
    {
        var context = new SerializationContext{AllowNullValues = allowNullValues, ByteOrder = byteOrder};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeArray()
    {
        var expected = new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<byte[]>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyArray()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<byte[]>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullArray()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<byte[]>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    public static IEnumerable<object?[]> ListData
    {
        get
        {
            yield return new object?[]
            { 
                typeof(List<byte>),
                new List<byte> {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
                ByteOrder.LittleEndian,
                false,
                new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}
            };
            yield return new object?[]
            { 
                typeof(List<byte>),
                new List<byte> {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
                ByteOrder.BigEndian,
                false,
                new byte[]{0x00, 0x00, 0x00, 0x10, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}
            };
            yield return new object?[]
            { 
                typeof(HashSet<byte>),
                new HashSet<byte> {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
                ByteOrder.LittleEndian,
                false,
                new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}
            };
            // yield return new object?[]
            // { 
            //     typeof(int[]),
            //     new int[] {1, 2, -3, -18},
            //     ByteOrder.LittleEndian,
            //     false,
            //     new byte[]{0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0xfd, 0xff, 0xff, 0xff, 0xee, 0xff, 0xff, 0xff}
            // };
            // yield return new object?[]
            // { 
            //     typeof(int[]),
            //     new int[] {1,2,-3,-18},
            //     ByteOrder.BigEndian,
            //     false,
            //     new byte[]{0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0xff, 0xff, 0xff, 0xfd, 0xff, 0xff, 0xff, 0xee}
            // };
            // yield return new object?[]
            // { 
            //     typeof(short[]),
            //     new short[] {1,2,-3,-18},
            //     ByteOrder.BigEndian,
            //     true,
            //     new byte[]{0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x02, 0xff, 0xfd, 0xff, 0xee}
            // };
            // yield return new object?[]
            // { 
            //     typeof(long[]),
            //     null,
            //     ByteOrder.BigEndian,
            //     true,
            //     new byte[]{0x00}
            // };
            // yield return new object?[]
            // { 
            //     typeof(short?[]),
            //     new short?[] {1,2,-3,-18, null},
            //     ByteOrder.BigEndian,
            //     true,
            //     new byte[]{0x01, 0x00, 0x00, 0x00, 0x05, 0x01, 0x00, 0x01, 0x01, 0x00, 0x02, 0x01, 0xff, 0xfd, 0x01, 0xff, 0xee, 0x00}
            // };
        }
    }

    [Theory]
    [MemberData(nameof(ListData))]
    public async Task SerializeLists(Type type, object value, ByteOrder byteOrder, bool allowNullValues, byte[] expected)
    {
        var context = new SerializationContext{AllowNullValues = allowNullValues, ByteOrder = byteOrder};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeList()
    {
        var expected = new List<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<List<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<List<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullList()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<List<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeIReadOnlyList()
    {
        var expected = new List<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<IReadOnlyList<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyIReadOnlyList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyList<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullIReadOnlyList()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyList<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeIList()
    {
        var expected = new List<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<IList<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyIList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<IList<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullIList()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<IList<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeICollection()
    {
        var expected = new List<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<ICollection<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyICollection()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<ICollection<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullICollection()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<ICollection<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeIReadOnlyCollection()
    {
        var expected = new List<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<IReadOnlyCollection<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyIReadOnlyCollection()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyCollection<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullIReadOnlyCollection()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyCollection<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeHashSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new HashSet<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        var expected = new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }


    [Fact]
    public async Task DeserializeHashSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new HashSet<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<HashSet<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyHashSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<HashSet<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullHashSet()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<HashSet<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeISet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new HashSet<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<ISet<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyISet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<ISet<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullISet()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<ISet<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeserializeIReadOnlySet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new HashSet<byte>{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<IReadOnlySet<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyIReadOnlySet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlySet<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullIReadOnlySet()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlySet<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeStack()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new Stack<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            value.Push(b);
        var expected = new byte[]{0x10, 0x00, 0x00, 0x00, 0x0f, 0x0e, 0x0d, 0x0c, 0x0b, 0x0a, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeStack()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new Stack<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            expected.Push(b);
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x0f, 0x0e, 0x0d, 0x0c, 0x0b, 0x0a, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00]);
        var value = await Serializer.FromStreamAsync<Stack<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyStack()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<Stack<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullStack()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<Stack<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeQueue()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new Queue<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            value.Enqueue(b);
        var expected = new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeQueue()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new Queue<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            expected.Enqueue(b);
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<Queue<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyQueue()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<Queue<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullQueue()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<Queue<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeSortedSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new SortedSet<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            value.Add(b);
        var expected = new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeSortedSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new SortedSet<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            expected.Add(b);
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<SortedSet<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptySortedSet()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<SortedSet<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullSortedSet()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<SortedSet<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeLinkedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new LinkedList<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            value.AddLast(b);
        var expected = new byte[]{0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeLinkedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new LinkedList<byte>();
        foreach(var b in new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f})
            expected.AddLast(b);
        using var stream = new MemoryStream([0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f]);
        var value = await Serializer.FromStreamAsync<LinkedList<byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyLinkedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<SortedSet<byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullLinkedList()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<SortedSet<byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new Dictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };
        var expected = new byte[]{0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new Dictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };

        using var stream = new MemoryStream([0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08]);
        var value = await Serializer.FromStreamAsync<Dictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<Dictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullDictionary()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<Dictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeSortedDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new SortedDictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };
        var expected = new byte[]{0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeSortedDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new Dictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };

        using var stream = new MemoryStream([0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08]);
        var value = await Serializer.FromStreamAsync<SortedDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptySortedDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<SortedDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullSortedDictionary()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<SortedDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeSortedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var value = new SortedList<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };
        var expected = new byte[]{0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeSortedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new SortedList<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };

        using var stream = new MemoryStream([0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08]);
        var value = await Serializer.FromStreamAsync<SortedList<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptySortedList()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<SortedList<byte,byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullSortedList()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<SortedList<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SerializeIReadOnlyDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        IReadOnlyDictionary<byte,byte> value = new Dictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };
        var expected = new byte[]{0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08};
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task DeserializeIReadOnlyDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        var expected = new Dictionary<byte, byte>()
        {
            {0x00, 0x0f},
            {0x01, 0x0e},
            {0x02, 0x0d},
            {0x03, 0x0c},
            {0x04, 0x0b},
            {0x05, 0x0a},
            {0x06, 0x09},
            {0x07, 0x08}
        };

        using var stream = new MemoryStream([0x08, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x0e, 0x02, 0x0d, 0x03, 0x0c, 0x04, 0x0b, 0x05, 0x0a, 0x06, 0x09, 0x07, 0x08]);
        var value = await Serializer.FromStreamAsync<IReadOnlyDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task DeserializeEmptyIRedOnlyDictionary()
    {
        var context = new SerializationContext{AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00, 0x00, 0x00, 0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact]
    public async Task DeserializeNullIReadOnlyDictionary()
    {
        var context = new SerializationContext{AllowNullValues = true, ByteOrder = ByteOrder.LittleEndian};
        using var stream = new MemoryStream([0x00]);
        var value = await Serializer.FromStreamAsync<IReadOnlyDictionary<byte,byte>>(stream, context, CancellationToken.None);
        Assert.Null(value);
    }
}