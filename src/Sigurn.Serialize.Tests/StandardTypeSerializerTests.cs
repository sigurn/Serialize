using System.Text;

namespace Sigurn.Serialize.Tests;

public class StandardTypeSerializerTests
{
    public static IEnumerable<object[]> StringData
    {
        get
        {
            var testString = "The quick brown fox jumps over the lazy dog";

            yield return new object[] { testString, 
                new SerializationContext
                { 
                    Encoding = Encoding.ASCII,
                    ByteOrder = ByteOrder.LittleEndian,
                    AllowNullValues = false
                }, new byte[] {0x2b, 0x00, 0x00, 0x00, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.UTF8,
                    ByteOrder = ByteOrder.LittleEndian,
                    AllowNullValues = false
                }, new byte[] {0x2b, 0x00, 0x00, 0x00, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] { testString, 
                new SerializationContext
                {
                    Encoding = Encoding.Unicode,
                    ByteOrder = ByteOrder.LittleEndian,
                    AllowNullValues = false
                }, new byte[] {0x56, 0x00, 0x00, 0x00, 0x54, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x71, 0x00, 0x75, 0x00, 0x69, 0x00, 0x63, 0x00, 0x6b, 0x00, 0x20, 0x00, 0x62, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x77, 0x00, 0x6e, 0x00, 0x20, 0x00, 0x66, 0x00, 0x6f, 0x00, 0x78, 0x00, 0x20, 0x00, 0x6a, 0x00, 0x75, 0x00, 0x6d, 0x00, 0x70, 0x00, 0x73, 0x00, 0x20, 0x00, 0x6f, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x20, 0x00, 0x74, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x6c, 0x00, 0x61, 0x00, 0x7a, 0x00, 0x79, 0x00, 0x20, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x67, 0x00}};

            yield return new object[] {testString,
                new SerializationContext
                {
                    Encoding = Encoding.ASCII,
                    ByteOrder = ByteOrder.BigEndian,
                    AllowNullValues = false
                }, new byte[] {0x00, 0x00, 0x00, 0x2b, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString,
                new SerializationContext
                {
                    Encoding = Encoding.UTF8,
                    ByteOrder = ByteOrder.BigEndian,
                    AllowNullValues =  false
                }, new byte[] {0x00, 0x00, 0x00, 0x2b, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.Unicode,
                    ByteOrder = ByteOrder.BigEndian,
                    AllowNullValues = false
                }, new byte[] {0x00, 0x00, 0x00, 0x56, 0x54, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x71, 0x00, 0x75, 0x00, 0x69, 0x00, 0x63, 0x00, 0x6b, 0x00, 0x20, 0x00, 0x62, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x77, 0x00, 0x6e, 0x00, 0x20, 0x00, 0x66, 0x00, 0x6f, 0x00, 0x78, 0x00, 0x20, 0x00, 0x6a, 0x00, 0x75, 0x00, 0x6d, 0x00, 0x70, 0x00, 0x73, 0x00, 0x20, 0x00, 0x6f, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x20, 0x00, 0x74, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x6c, 0x00, 0x61, 0x00, 0x7a, 0x00, 0x79, 0x00, 0x20, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x67, 0x00}};

            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.ASCII, 
                    ByteOrder = ByteOrder.LittleEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x2b, 0x00, 0x00, 0x00, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.UTF8, 
                    ByteOrder = ByteOrder.LittleEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x2b, 0x00, 0x00, 0x00, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.Unicode, 
                    ByteOrder = ByteOrder.LittleEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x56, 0x00, 0x00, 0x00, 0x54, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x71, 0x00, 0x75, 0x00, 0x69, 0x00, 0x63, 0x00, 0x6b, 0x00, 0x20, 0x00, 0x62, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x77, 0x00, 0x6e, 0x00, 0x20, 0x00, 0x66, 0x00, 0x6f, 0x00, 0x78, 0x00, 0x20, 0x00, 0x6a, 0x00, 0x75, 0x00, 0x6d, 0x00, 0x70, 0x00, 0x73, 0x00, 0x20, 0x00, 0x6f, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x20, 0x00, 0x74, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x6c, 0x00, 0x61, 0x00, 0x7a, 0x00, 0x79, 0x00, 0x20, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x67, 0x00}};

            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.ASCII, 
                    ByteOrder = ByteOrder.BigEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x00, 0x00, 0x00, 0x2b, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.UTF8, 
                    ByteOrder = ByteOrder.BigEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x00, 0x00, 0x00, 0x2b, 0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6B, 0x20, 0x62, 0x72, 0x6F, 0x77, 0x6E, 0x20, 0x66, 0x6F, 0x78, 0x20, 0x6A, 0x75, 0x6D, 0x70, 0x73, 0x20, 0x6F, 0x76, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x61, 0x7A, 0x79, 0x20, 0x64, 0x6F, 0x67}};
            yield return new object[] {testString, 
                new SerializationContext
                {
                    Encoding = Encoding.Unicode, 
                    ByteOrder = ByteOrder.BigEndian, 
                    AllowNullValues = true
                }, new byte[] {0x01, 0x00, 0x00, 0x00, 0x56, 0x54, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x71, 0x00, 0x75, 0x00, 0x69, 0x00, 0x63, 0x00, 0x6b, 0x00, 0x20, 0x00, 0x62, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x77, 0x00, 0x6e, 0x00, 0x20, 0x00, 0x66, 0x00, 0x6f, 0x00, 0x78, 0x00, 0x20, 0x00, 0x6a, 0x00, 0x75, 0x00, 0x6d, 0x00, 0x70, 0x00, 0x73, 0x00, 0x20, 0x00, 0x6f, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x20, 0x00, 0x74, 0x00, 0x68, 0x00, 0x65, 0x00, 0x20, 0x00, 0x6c, 0x00, 0x61, 0x00, 0x7a, 0x00, 0x79, 0x00, 0x20, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x67, 0x00}};
        }
    }

    [Theory]
    [MemberData(nameof(StringData))]
    public async Task SerializeStringValueAsync(string value, SerializationContext context, byte[] array)
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(StringData))]
    public async Task DeserializeStringValueAsync(string expected, SerializationContext context, byte[] array)
    {
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync<string>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    public static IEnumerable<object[]> GuidData
    {
        get
        {
            yield return new object[] {Guid.Empty, ByteOrder.LittleEndian, UuidForm.Microsoft, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.LittleEndian, UuidForm.Microsoft, new byte[] {0xd5, 0x4f, 0x30, 0x6f, 0x04, 0x29, 0x5c, 0x47, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
            yield return new object[] {Guid.Empty, ByteOrder.BigEndian, UuidForm.Microsoft, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.BigEndian, UuidForm.Microsoft, new byte[] {0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};

            yield return new object[] {Guid.Empty, ByteOrder.LittleEndian, UuidForm.Linux, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.LittleEndian, UuidForm.Linux, new byte[] {0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
            yield return new object[] {Guid.Empty, ByteOrder.BigEndian, UuidForm.Linux, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.BigEndian, UuidForm.Linux, new byte[] {0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
        }
    }

    [Theory]
    [MemberData(nameof(GuidData))]
    public async Task SerializeGuidValueAsync(Guid value, ByteOrder byteOrder, UuidForm uuidForm, byte[] array)
    {
        var context = new SerializationContext{ UuidForm = uuidForm, ByteOrder = byteOrder };
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(GuidData))]
    public async Task DeserializeGuidValueAsync(Guid expected, ByteOrder byteOrder, UuidForm uuidForm, byte[] array)
    {
        var context = new SerializationContext{ UuidForm = uuidForm, ByteOrder = byteOrder };
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync<Guid>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    public static IEnumerable<object?[]> NullableGuidData
    {
        get
        {
            yield return new object?[] {null, ByteOrder.LittleEndian, UuidForm.Microsoft, new byte[] {0x00}};
            yield return new object?[] {Guid.Empty, ByteOrder.LittleEndian, UuidForm.Microsoft, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.LittleEndian, UuidForm.Microsoft, new byte[] {0x01, 0xd5, 0x4f, 0x30, 0x6f, 0x04, 0x29, 0x5c, 0x47, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
            yield return new object?[] {Guid.Empty, ByteOrder.BigEndian, UuidForm.Microsoft, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.BigEndian, UuidForm.Microsoft, new byte[] {0x01, 0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};

            yield return new object?[] {null, ByteOrder.LittleEndian, UuidForm.Linux, new byte[] {0x00}};
            yield return new object?[] {Guid.Empty, ByteOrder.LittleEndian, UuidForm.Linux, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.LittleEndian, UuidForm.Linux, new byte[] {0x01, 0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
            yield return new object?[] {Guid.Empty, ByteOrder.BigEndian, UuidForm.Linux, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {new Guid("6f304fd5-2904-475c-80b3-4db0f3e2b58a"), ByteOrder.BigEndian, UuidForm.Linux, new byte[] {0x01, 0x6f, 0x30, 0x4f, 0xd5, 0x29, 0x04, 0x47, 0x5c, 0x80, 0xb3, 0x4d, 0xb0, 0xf3, 0xe2, 0xb5, 0x8a}};
        }
    }

    [Theory]
    [MemberData(nameof(NullableGuidData))]
    public async Task SerializeNullableGuidValueAsync(Guid? value, ByteOrder byteOrder, UuidForm uuidForm, byte[] array)
    {
        var context = new SerializationContext{ UuidForm = uuidForm, ByteOrder = byteOrder, AllowNullValues = true };
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, typeof(Guid?), value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(NullableGuidData))]
    public async Task DeserializeNullableGuidValueAsync(Guid? expected, ByteOrder byteOrder, UuidForm uuidForm, byte[] array)
    {
        var context = new SerializationContext{ UuidForm = uuidForm, ByteOrder = byteOrder, AllowNullValues = true };
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync<Guid?>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

   public static IEnumerable<object?[]> VersionData
    {
        get
        {
            yield return new object[] {new Version(), ByteOrder.LittleEndian, false, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(193, 512), ByteOrder.LittleEndian, false, new byte[] {0xc1, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(1, 2, 3), ByteOrder.LittleEndian, false, new byte[] {0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(4, 5, 6, 7), ByteOrder.LittleEndian, false, new byte[] {0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00}};

            yield return new object?[] {null, ByteOrder.LittleEndian, true, new byte[] {0x00}};
            yield return new object[] {new Version(), ByteOrder.LittleEndian, true, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(193, 512), ByteOrder.LittleEndian, true, new byte[] {0x01, 0xc1, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(1, 2, 3), ByteOrder.LittleEndian, true, new byte[] {0x01, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(4, 5, 6, 7), ByteOrder.LittleEndian, true, new byte[] {0x01, 0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00}};

            yield return new object[] {new Version(), ByteOrder.BigEndian, false, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(193, 512), ByteOrder.BigEndian, false, new byte[] {0x00, 0x00, 0x00, 0xc1, 0x00, 0x00, 0x02, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(1, 2, 3), ByteOrder.BigEndian, false, new byte[] {0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(4, 5, 6, 7), ByteOrder.BigEndian, false, new byte[] {0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x07}};

            yield return new object?[] {null, ByteOrder.BigEndian, true, new byte[] {0x00}};
            yield return new object[] {new Version(), ByteOrder.BigEndian, true, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(193, 512), ByteOrder.BigEndian, true, new byte[] {0x01, 0x00, 0x00, 0x00, 0xc1, 0x00, 0x00, 0x02, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(1, 2, 3), ByteOrder.BigEndian, true, new byte[] {0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {new Version(4, 5, 6, 7), ByteOrder.BigEndian, true, new byte[] {0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x07}};
        }
    }

    [Theory]
    [MemberData(nameof(VersionData))]
    public async Task SerializeVersionValueAsync(Version value, ByteOrder byteOrder, bool allowNullValues, byte[] array)
    {
        var context = new SerializationContext{ ByteOrder = byteOrder, AllowNullValues = allowNullValues };
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(VersionData))]
    public async Task DeserializeVersionValueAsync(Version expected, ByteOrder byteOrder, bool allowNullValues, byte[] array)
    {
        var context = new SerializationContext{ ByteOrder = byteOrder, AllowNullValues = allowNullValues };
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync<Version>(stream, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    public static IEnumerable<object?[]> NullableTypeData
    {
        get
        {
            yield return new object?[] { typeof(bool?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(bool?), true, ByteOrder.LittleEndian, new byte[] {0x01, 0x01}};
            yield return new object?[] {typeof(bool?), false, ByteOrder.LittleEndian, new byte[] {0x01, 0x00}};

            yield return new object?[] { typeof(bool?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(bool?), true, ByteOrder.BigEndian, new byte[] {0x01, 0x01}};
            yield return new object?[] {typeof(bool?), false, ByteOrder.BigEndian, new byte[] {0x01, 0x00}};

            yield return new object?[] {typeof(sbyte?), null, ByteOrder.LittleEndian, new byte[] {0x00}};            
            yield return new object?[] {typeof(sbyte?), sbyte.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x80}};
            yield return new object?[] {typeof(sbyte?), sbyte.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x7f}};
            yield return new object?[] {typeof(sbyte?), (sbyte)0x4a, ByteOrder.LittleEndian, new byte[] {0x01, 0x4a}};

            yield return new object?[] {typeof(sbyte?), null, ByteOrder.BigEndian, new byte[] {0x00}};            
            yield return new object?[] {typeof(sbyte?), sbyte.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80}};
            yield return new object?[] {typeof(sbyte?), sbyte.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f}};
            yield return new object?[] {typeof(sbyte?), (sbyte)0x4a, ByteOrder.BigEndian, new byte[] {0x01, 0x4a}};

            yield return new object?[] {typeof(byte?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(byte?), byte.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00}};
            yield return new object?[] {typeof(byte?), byte.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff}};
            yield return new object?[] {typeof(byte?), (byte)0x9c, ByteOrder.LittleEndian, new byte[] {0x01, 0x9c}};

            yield return new object?[] {typeof(byte?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(byte?), byte.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00}};
            yield return new object?[] {typeof(byte?), byte.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff}};
            yield return new object?[] {typeof(byte?), (byte)0x9c, ByteOrder.BigEndian, new byte[] {0x01, 0x9c}};

            yield return new object?[] {typeof(short?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(short?), short.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x80}};
            yield return new object?[] {typeof(short?), short.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0x7f}};
            yield return new object?[] {typeof(short?), (short)19099, ByteOrder.LittleEndian, new byte[]{0x01, 0x9b, 0x4a}};

            yield return new object?[] {typeof(short?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(short?), short.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00}};
            yield return new object?[] {typeof(short?), short.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xff}};
            yield return new object?[] {typeof(short?), (short)19099, ByteOrder.BigEndian, new byte[]{0x01, 0x4a, 0x9b}};

            yield return new object?[] {typeof(ushort?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(ushort?), ushort.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00}};
            yield return new object?[] {typeof(ushort?), ushort.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff}};
            yield return new object?[] {typeof(ushort?), (ushort)19099, ByteOrder.LittleEndian, new byte[] {0x01, 0x9b, 0x4a}};

            yield return new object?[] {typeof(ushort?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(ushort?), ushort.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00}};
            yield return new object?[] {typeof(ushort?), ushort.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xff}};
            yield return new object?[] {typeof(ushort?), (ushort)19099, ByteOrder.BigEndian, new byte[] {0x01, 0x4a, 0x9b}};

            yield return new object?[] {typeof(int?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(int?), int.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(int?), int.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0x7f}};
            yield return new object?[] {typeof(int?), (int)0x66778899, ByteOrder.LittleEndian, new byte[] {0x01, 0x99, 0x88, 0x77, 0x66}};

            yield return new object?[] {typeof(int?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(int?), int.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(int?), int.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(int?), (int)0x66778899, ByteOrder.BigEndian, new byte[] {0x01, 0x66, 0x77, 0x88, 0x99}};
   
            yield return new object?[] {typeof(uint?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(uint?), uint.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(uint?), uint.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(uint?), (uint)0xaabbccdd, ByteOrder.LittleEndian, new byte[] {0x01, 0xdd, 0xcc, 0xbb, 0xaa}};

            yield return new object?[] {typeof(uint?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(uint?), uint.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(uint?), uint.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(uint?), (uint)0xaabbccdd, ByteOrder.BigEndian, new byte[] {0x01, 0xaa, 0xbb, 0xcc, 0xdd}};

            yield return new object?[] {typeof(long?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(long?), long.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(long?), long.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f}};
            yield return new object?[] {typeof(long?), (long)0x66778899aabbccdd, ByteOrder.LittleEndian, new byte[] {0x01, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88, 0x77, 0x66}};

            yield return new object?[] {typeof(long?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(long?), long.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(long?), long.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(long?), (long)0x66778899aabbccdd, ByteOrder.BigEndian, new byte[] {0x01, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd}};

            yield return new object?[] {typeof(ulong?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(ulong?), ulong.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(ulong?), ulong.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(ulong?), (ulong)0x8899aabbccddeeff, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xee, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88}};

            yield return new object?[] {typeof(ulong?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(ulong?), ulong.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(ulong?), ulong.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(ulong?), (ulong)0x8899aabbccddeeff, ByteOrder.BigEndian, new byte[] {0x01, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff}};

            yield return new object?[] {typeof(float?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(float?), float.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0x7f, 0xff}};
            yield return new object?[] {typeof(float?), float.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0x7f, 0x7f}};
            yield return new object?[] {typeof(float?), float.NaN, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0xc0, 0xff}};
            yield return new object?[] {typeof(float?), float.PositiveInfinity, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x80, 0x7f}};
            yield return new object?[] {typeof(float?), float.NegativeInfinity, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x80, 0xff}};
            yield return new object?[] {typeof(float?), float.NegativeZero, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(float?), (float)Math.PI, ByteOrder.LittleEndian, new byte[] {0x01, 0xdb, 0x0f, 0x49, 0x40}};

            yield return new object?[] {typeof(float?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(float?), float.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0x7f, 0xff, 0xff}};
            yield return new object?[] {typeof(float?), float.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0x7f, 0xff, 0xff}};
            yield return new object?[] {typeof(float?), float.NaN, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xc0, 0x00, 0x00}};
            yield return new object?[] {typeof(float?), float.PositiveInfinity, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0x80, 0x00, 0x00}};
            yield return new object?[] {typeof(float?), float.NegativeInfinity, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0x80, 0x00, 0x00}};
            yield return new object?[] {typeof(float?), float.NegativeZero, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(float?), (float)Math.PI, ByteOrder.BigEndian, new byte[] {0x01, 0x40, 0x49 ,0x0f, 0xdb}};

            yield return new object?[] {typeof(double?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(double?), double.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0xff}};
            yield return new object?[] {typeof(double?), double.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0x7f}};
            yield return new object?[] {typeof(double?), double.NaN, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff}};
            yield return new object?[] {typeof(double?), double.PositiveInfinity, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf0, 0x7f}};
            yield return new object?[] {typeof(double?), double.NegativeInfinity, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf0, 0xff}};
            yield return new object?[] {typeof(double?), double.NegativeZero, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(double?), Math.PI, ByteOrder.LittleEndian, new byte[] {0x01, 0x18, 0x2d, 0x44, 0x54, 0xfb, 0x21, 0x09, 0x40}};

            yield return new object?[] {typeof(double?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(double?), double.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xef, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(double?), double.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xef, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(double?), double.NaN, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(double?), double.PositiveInfinity, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(double?), double.NegativeInfinity, ByteOrder.BigEndian, new byte[] {0x01, 0xff, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(double?), double.NegativeZero, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(double?), Math.PI, ByteOrder.BigEndian, new byte[] {0x01, 0x40, 0x09, 0x21, 0xfb, 0x54, 0x44, 0x2d, 0x18}};

            yield return new object?[] {typeof(decimal?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(decimal?), decimal.Zero, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(decimal?), decimal.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(decimal?), decimal.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(decimal?), (decimal)Math.PI, ByteOrder.LittleEndian, new byte[] {0x01, 0x83, 0x24, 0x6a, 0xe7, 0xb9, 0x1d, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e, 0x00}};

            yield return new object?[] {typeof(decimal?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(decimal?), decimal.Zero, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(decimal?), decimal.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(decimal?), decimal.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(decimal?), (decimal)Math.PI, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x1d, 0xb9, 0xe7, 0x6a, 0x24, 0x83}};

            yield return new object?[] {typeof(DateTime?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(DateTime?), DateTime.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(DateTime?), DateTime.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0x3f, 0x37, 0xf4, 0x75, 0x28, 0xca, 0x2b}};
            yield return new object?[] {typeof(DateTime?), new DateTime(2024, 03, 14, 13, 45, 26), ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x5f, 0xf4, 0x00, 0x2d, 0x44, 0xdc, 0x08}};

            yield return new object?[] {typeof(DateTime?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(DateTime?), DateTime.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(DateTime?), DateTime.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff}};
            yield return new object?[] {typeof(DateTime?), new DateTime(2024, 03, 14, 13, 45, 26), ByteOrder.BigEndian, new byte[] {0x01, 0x08, 0xdc, 0x44, 0x2d, 0x00, 0xf4, 0x5f, 0x00}};

            yield return new object?[] {typeof(TimeSpan?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(TimeSpan?), TimeSpan.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object?[] {typeof(TimeSpan?), TimeSpan.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f}};
            yield return new object?[] {typeof(TimeSpan?), new TimeSpan(165, 18, 35, 48, 739), ByteOrder.LittleEndian, new byte[] {0x01, 0x30, 0x9d, 0xb7, 0x36, 0x44, 0x82, 0x00, 0x00}};

            yield return new object?[] {typeof(TimeSpan?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(TimeSpan?), TimeSpan.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(TimeSpan?), TimeSpan.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object?[] {typeof(TimeSpan?), new TimeSpan(165, 18, 35, 48, 739), ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x82, 0x44, 0x36, 0xb7, 0x9d, 0x30}};

            yield return new object?[] {typeof(DateOnly?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(DateOnly?), DateOnly.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x01, 0x00, 0x01, 0x01}};
            yield return new object?[] {typeof(DateOnly?), DateOnly.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x0f, 0x27, 0x0c, 0x1f}};
            yield return new object?[] {typeof(DateOnly?), new DateOnly(2024, 3, 14), ByteOrder.LittleEndian, new byte[] {0x01, 0xe8, 0x07, 0x03, 0x0e}};

            yield return new object?[] {typeof(DateOnly?), null, ByteOrder.BigEndian, new byte[] {0x00,}};
            yield return new object?[] {typeof(DateOnly?), DateOnly.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x01, 0x01, 0x01}};
            yield return new object?[] {typeof(DateOnly?), DateOnly.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x27, 0x0f, 0x0c, 0x1f}};
            yield return new object?[] {typeof(DateOnly?), new DateOnly(2024, 3, 14), ByteOrder.BigEndian, new byte[] {0x01, 0x07, 0xe8, 0x03, 0x0e}};

            yield return new object?[] {typeof(TimeOnly?), null, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(TimeOnly?), TimeOnly.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(TimeOnly?), TimeOnly.MaxValue, ByteOrder.LittleEndian, new byte[] {0x01, 0xff, 0xbf, 0x69, 0x2a, 0xc9, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(TimeOnly?), new TimeOnly(23, 15, 53, 978, 863), ByteOrder.LittleEndian, new byte[] {0x01, 0x56, 0xf7, 0x42, 0x01, 0xc3, 0x00, 0x00, 0x00}};

            yield return new object?[] {typeof(TimeOnly?), null, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object?[] {typeof(TimeOnly?), TimeOnly.MinValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object?[] {typeof(TimeOnly?), TimeOnly.MaxValue, ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0xc9, 0x2a, 0x69, 0xbf, 0xff}};
            yield return new object?[] {typeof(TimeOnly?), new TimeOnly(23, 15, 53, 978, 863), ByteOrder.BigEndian, new byte[] {0x01, 0x00, 0x00, 0x00, 0xc3, 0x01, 0x42, 0xf7, 0x56}};
      }
    }

    [Theory]
    [MemberData(nameof(NullableTypeData))]
    public async Task SerializeNullableTypeValueAsync(Type type, object? value, ByteOrder byteOrder, byte[] array)
    {
        var context = new SerializationContext{ AllowNullValues = true, ByteOrder = byteOrder };
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(NullableTypeData))]
    public async Task DeserializeNullableTypeValueAsync(Type type, object? expected, ByteOrder byteOrder, byte[] array)
    {
        var context = new SerializationContext{ AllowNullValues = true, ByteOrder = byteOrder };
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync(stream, type, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }

    public static IEnumerable<object?[]> TypeData
    {
        get
        {
            yield return new object[] {typeof(bool), true, ByteOrder.LittleEndian, new byte[] {0x01}};
            yield return new object[] {typeof(bool), false, ByteOrder.LittleEndian, new byte[] {0x00}};

            yield return new object[] {typeof(bool), true, ByteOrder.BigEndian, new byte[] {0x01}};
            yield return new object[] {typeof(bool), false, ByteOrder.BigEndian, new byte[] {0x00}};

            yield return new object[] {typeof(sbyte), sbyte.MinValue, ByteOrder.LittleEndian, new byte[] {0x80}};
            yield return new object[] {typeof(sbyte), sbyte.MaxValue, ByteOrder.LittleEndian, new byte[] {0x7f}};
            yield return new object[] {typeof(sbyte), (sbyte)0x4a, ByteOrder.LittleEndian, new byte[] {0x4a}};

            yield return new object[] {typeof(sbyte), sbyte.MinValue, ByteOrder.BigEndian, new byte[] {0x80}};
            yield return new object[] {typeof(sbyte), sbyte.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f}};
            yield return new object[] {typeof(sbyte), (sbyte)0x4a, ByteOrder.BigEndian, new byte[] {0x4a}};

            yield return new object[] {typeof(byte), byte.MinValue, ByteOrder.LittleEndian, new byte[] {0x00}};
            yield return new object[] {typeof(byte), byte.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff}};
            yield return new object[] {typeof(byte), (byte)0x9c, ByteOrder.LittleEndian, new byte[] {0x9c}};

            yield return new object[] {typeof(byte), byte.MinValue, ByteOrder.BigEndian, new byte[] {0x00}};
            yield return new object[] {typeof(byte), byte.MaxValue, ByteOrder.BigEndian, new byte[] {0xff}};
            yield return new object[] {typeof(byte), (byte)0x9c, ByteOrder.BigEndian, new byte[] {0x9c}};

            yield return new object[] {typeof(short), short.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x80}};
            yield return new object[] {typeof(short), short.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0x7f}};
            yield return new object[] {typeof(short), (short)19099, ByteOrder.LittleEndian, new byte[]{0x9b, 0x4a}};

            yield return new object[] {typeof(short), short.MinValue, ByteOrder.BigEndian, new byte[] {0x80, 0x00}};
            yield return new object[] {typeof(short), short.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0xff}};
            yield return new object[] {typeof(short), (short)19099, ByteOrder.BigEndian, new byte[]{0x4a, 0x9b}};

            yield return new object[] {typeof(ushort), ushort.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00}};
            yield return new object[] {typeof(ushort), ushort.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff}};
            yield return new object[] {typeof(ushort), (ushort)19099, ByteOrder.LittleEndian, new byte[] {0x9b, 0x4a}};

            yield return new object[] {typeof(ushort), ushort.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00}};
            yield return new object[] {typeof(ushort), ushort.MaxValue, ByteOrder.BigEndian, new byte[] {0xff, 0xff}};
            yield return new object[] {typeof(ushort), (ushort)19099, ByteOrder.BigEndian, new byte[] {0x4a, 0x9b}};

            yield return new object[] {typeof(int), int.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(int), int.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0x7f}};
            yield return new object[] {typeof(int), (int)0x66778899, ByteOrder.LittleEndian, new byte[] {0x99, 0x88, 0x77, 0x66}};

            yield return new object[] {typeof(int), int.MinValue, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(int), int.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(int), (int)0x66778899, ByteOrder.BigEndian, new byte[] {0x66, 0x77, 0x88, 0x99}};
   
            yield return new object[] {typeof(uint), uint.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(uint), uint.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(uint), (uint)0xaabbccdd, ByteOrder.LittleEndian, new byte[] {0xdd, 0xcc, 0xbb, 0xaa}};

            yield return new object[] {typeof(uint), uint.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(uint), uint.MaxValue, ByteOrder.BigEndian, new byte[] {0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(uint), (uint)0xaabbccdd, ByteOrder.BigEndian, new byte[] {0xaa, 0xbb, 0xcc, 0xdd}};

            yield return new object[] {typeof(long), long.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(long), long.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f}};
            yield return new object[] {typeof(long), (long)0x66778899aabbccdd, ByteOrder.LittleEndian, new byte[] {0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88, 0x77, 0x66}};

            yield return new object[] {typeof(long), long.MinValue, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(long), long.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(long), (long)0x66778899aabbccdd, ByteOrder.BigEndian, new byte[] {0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd}};

            yield return new object[] {typeof(ulong), ulong.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(ulong), ulong.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(ulong), (ulong)0x8899aabbccddeeff, ByteOrder.LittleEndian, new byte[] {0xff, 0xee, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88}};

            yield return new object[] {typeof(ulong), ulong.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(ulong), ulong.MaxValue, ByteOrder.BigEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(ulong), (ulong)0x8899aabbccddeeff, ByteOrder.BigEndian, new byte[] {0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff}};

            yield return new object[] {typeof(float), float.MinValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0x7f, 0xff}};
            yield return new object[] {typeof(float), float.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0x7f, 0x7f}};
            yield return new object[] {typeof(float), float.NaN, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0xc0, 0xff}};
            yield return new object[] {typeof(float), float.PositiveInfinity, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x80, 0x7f}};
            yield return new object[] {typeof(float), float.NegativeInfinity, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x80, 0xff}};
            yield return new object[] {typeof(float), float.NegativeZero, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(float), (float)Math.PI, ByteOrder.LittleEndian, new byte[] {0xdb, 0x0f, 0x49, 0x40}};

            yield return new object[] {typeof(float), float.MinValue, ByteOrder.BigEndian, new byte[] {0xff, 0x7f, 0xff, 0xff}};
            yield return new object[] {typeof(float), float.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0x7f, 0xff, 0xff}};
            yield return new object[] {typeof(float), float.NaN, ByteOrder.BigEndian, new byte[] {0xff, 0xc0, 0x00, 0x00}};
            yield return new object[] {typeof(float), float.PositiveInfinity, ByteOrder.BigEndian, new byte[] {0x7f, 0x80, 0x00, 0x00}};
            yield return new object[] {typeof(float), float.NegativeInfinity, ByteOrder.BigEndian, new byte[] {0xff, 0x80, 0x00, 0x00}};
            yield return new object[] {typeof(float), float.NegativeZero, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(float), (float)Math.PI, ByteOrder.BigEndian, new byte[] {0x40, 0x49 ,0x0f, 0xdb}};

            yield return new object[] {typeof(double), double.MinValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0xff}};
            yield return new object[] {typeof(double), double.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0x7f}};
            yield return new object[] {typeof(double), double.NaN, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff}};
            yield return new object[] {typeof(double), double.PositiveInfinity, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf0, 0x7f}};
            yield return new object[] {typeof(double), double.NegativeInfinity, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf0, 0xff}};
            yield return new object[] {typeof(double), double.NegativeZero, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(double), Math.PI, ByteOrder.LittleEndian, new byte[] {0x18, 0x2d, 0x44, 0x54, 0xfb, 0x21, 0x09, 0x40}};

            yield return new object[] {typeof(double), double.MinValue, ByteOrder.BigEndian, new byte[] {0xff, 0xef, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(double), double.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0xef, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(double), double.NaN, ByteOrder.BigEndian, new byte[] {0xff, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(double), double.PositiveInfinity, ByteOrder.BigEndian, new byte[] {0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(double), double.NegativeInfinity, ByteOrder.BigEndian, new byte[] {0xff, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(double), double.NegativeZero, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(double), Math.PI, ByteOrder.BigEndian, new byte[] {0x40, 0x09, 0x21, 0xfb, 0x54, 0x44, 0x2d, 0x18}};

            yield return new object[] {typeof(decimal), decimal.Zero, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(decimal), decimal.MinValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(decimal), decimal.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(decimal), (decimal)Math.PI, ByteOrder.LittleEndian, new byte[] {0x83, 0x24, 0x6a, 0xe7, 0xb9, 0x1d, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e, 0x00}};

            yield return new object[] {typeof(decimal), decimal.Zero, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(decimal), decimal.MinValue, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(decimal), decimal.MaxValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(decimal), (decimal)Math.PI, ByteOrder.BigEndian, new byte[] {0x00, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x1d, 0xb9, 0xe7, 0x6a, 0x24, 0x83}};

            yield return new object[] {typeof(DateTime), DateTime.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(DateTime), DateTime.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0x3f, 0x37, 0xf4, 0x75, 0x28, 0xca, 0x2b}};
            yield return new object[] {typeof(DateTime), new DateTime(2024, 03, 14, 13, 45, 26), ByteOrder.LittleEndian, new byte[] {0x00, 0x5f, 0xf4, 0x00, 0x2d, 0x44, 0xdc, 0x08}};

            yield return new object[] {typeof(DateTime), DateTime.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(DateTime), DateTime.MaxValue, ByteOrder.BigEndian, new byte[] {0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff}};
            yield return new object[] {typeof(DateTime), new DateTime(2024, 03, 14, 13, 45, 26), ByteOrder.BigEndian, new byte[] {0x08, 0xdc, 0x44, 0x2d, 0x00, 0xf4, 0x5f, 0x00}};

            yield return new object[] {typeof(TimeSpan), TimeSpan.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80}};
            yield return new object[] {typeof(TimeSpan), TimeSpan.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f}};
            yield return new object[] {typeof(TimeSpan), new TimeSpan(165, 18, 35, 48, 739), ByteOrder.LittleEndian, new byte[] {0x30, 0x9d, 0xb7, 0x36, 0x44, 0x82, 0x00, 0x00}};

            yield return new object[] {typeof(TimeSpan), TimeSpan.MinValue, ByteOrder.BigEndian, new byte[] {0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(TimeSpan), TimeSpan.MaxValue, ByteOrder.BigEndian, new byte[] {0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}};
            yield return new object[] {typeof(TimeSpan), new TimeSpan(165, 18, 35, 48, 739), ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x82, 0x44, 0x36, 0xb7, 0x9d, 0x30}};

            yield return new object[] {typeof(DateOnly), DateOnly.MinValue, ByteOrder.LittleEndian, new byte[] {0x01, 0x00, 0x01, 0x01}};
            yield return new object[] {typeof(DateOnly), DateOnly.MaxValue, ByteOrder.LittleEndian, new byte[] {0x0f, 0x27, 0x0c, 0x1f}};
            yield return new object[] {typeof(DateOnly), new DateOnly(2024, 3, 14), ByteOrder.LittleEndian, new byte[] {0xe8, 0x07, 0x03, 0x0e}};

            yield return new object[] {typeof(DateOnly), DateOnly.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x01, 0x01, 0x01}};
            yield return new object[] {typeof(DateOnly), DateOnly.MaxValue, ByteOrder.BigEndian, new byte[] {0x27, 0x0f, 0x0c, 0x1f}};
            yield return new object[] {typeof(DateOnly), new DateOnly(2024, 3, 14), ByteOrder.BigEndian, new byte[] {0x07, 0xe8, 0x03, 0x0e}};

            yield return new object[] {typeof(TimeOnly), TimeOnly.MinValue, ByteOrder.LittleEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(TimeOnly), TimeOnly.MaxValue, ByteOrder.LittleEndian, new byte[] {0xff, 0xbf, 0x69, 0x2a, 0xc9, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(TimeOnly), new TimeOnly(23, 15, 53, 978, 863), ByteOrder.LittleEndian, new byte[] {0x56, 0xf7, 0x42, 0x01, 0xc3, 0x00, 0x00, 0x00}};

            yield return new object[] {typeof(TimeOnly), TimeOnly.MinValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}};
            yield return new object[] {typeof(TimeOnly), TimeOnly.MaxValue, ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0xc9, 0x2a, 0x69, 0xbf, 0xff}};
            yield return new object[] {typeof(TimeOnly), new TimeOnly(23, 15, 53, 978, 863), ByteOrder.BigEndian, new byte[] {0x00, 0x00, 0x00, 0xc3, 0x01, 0x42, 0xf7, 0x56}};
      }
    }

    [Theory]
    [MemberData(nameof(TypeData))]
    public async Task SerializeTypeValueAsync(Type type, object? value, ByteOrder byteOrder, byte[] array)
    {
        var context = new SerializationContext{ AllowNullValues = false, ByteOrder = byteOrder };
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        Assert.Equal(array, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(TypeData))]
    public async Task DeserializeTypeValueAsync(Type type, object expected, ByteOrder byteOrder, byte[] array)
    {
        var context = new SerializationContext{ AllowNullValues = false, ByteOrder = byteOrder };
        using var stream = new MemoryStream(array);
        var value = await Serializer.FromStreamAsync(stream, type, context, CancellationToken.None);
        Assert.Equal(expected, value);
    }
}