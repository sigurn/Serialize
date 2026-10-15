using Xunit;

namespace Sigurn.Serialize.Tests;

record CountingContext : SerializationContext
{
    public int DoubleLookups { get; private set; }

    public override ITypeSerializer? FindTypeSerializer(Type type)
    {
        if (type == typeof(double))
            DoubleLookups++;

        return base.FindTypeSerializer(type);
    }
}

class TypeWithoutSerializer
{
}

record TypeRegisteredLate();

class TypeRegisteredLateSerializer : ITypeSerializer<TypeRegisteredLate>
{
    public ValueTask<TypeRegisteredLate> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new TypeRegisteredLate());

    public ValueTask ToStreamAsync(Stream stream, TypeRegisteredLate value, SerializationContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}

record TypeRegisteredTwice();

class TypeRegisteredTwiceSerializer : ITypeSerializer<TypeRegisteredTwice>
{
    public ValueTask<TypeRegisteredTwice> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new TypeRegisteredTwice());

    public ValueTask ToStreamAsync(Stream stream, TypeRegisteredTwice value, SerializationContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}

public sealed class Tally : ISerializable
{
    public int Value { get; set; }

    public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
        Serializer.ToStreamAsync(stream, Value, context, cancellationToken);

    public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
        Value = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
}

public class LookupTests
{
    private const int Count = 1000;

    [Fact]
    public async Task AnArray_LooksUpTheSerializerOfItsElementsOnce_WhenItIsWritten()
    {
        var context = new CountingContext { AllowNullValues = false };

        await Serializer.ToStreamAsync(Stream.Null, new double[Count], context, CancellationToken.None);

        Assert.Equal(1, context.DoubleLookups);
    }

    [Fact]
    public async Task AnArray_LooksUpTheSerializerOfItsElementsOnce_WhenItIsRead()
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, new double[Count], SerializationContext.Default with { AllowNullValues = false }, CancellationToken.None);
        stream.Position = 0;
        var context = new CountingContext { AllowNullValues = false };

        var read = await Serializer.FromStreamAsync<double[]>(stream, context, CancellationToken.None);

        Assert.Equal(Count, read!.Length);
        Assert.Equal(1, context.DoubleLookups);
    }

    [Fact]
    public async Task AList_LooksUpTheSerializerOfItsElementsOnce()
    {
        var context = new CountingContext { AllowNullValues = false };
        using var stream = new MemoryStream();

        await Serializer.ToStreamAsync(stream, Enumerable.Repeat(0.5, Count).ToList(), context, CancellationToken.None);
        stream.Position = 0;
        await Serializer.FromStreamAsync<List<double>>(stream, context, CancellationToken.None);

        Assert.Equal(2, context.DoubleLookups);
    }

    [Fact]
    public async Task AnEmptyArrayOfATypeNoSerializerKnows_IsStillWrittenAndRead()
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = ByteOrder.LittleEndian };
        using var stream = new MemoryStream();

        await Serializer.ToStreamAsync(stream, Array.Empty<TypeWithoutSerializer>(), context, CancellationToken.None);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, stream.ToArray());
        stream.Position = 0;

        Assert.Empty((await Serializer.FromStreamAsync<TypeWithoutSerializer[]>(stream, context, CancellationToken.None))!);
    }

    [Fact]
    public async Task ASerializerRegisteredAfterOthersWereUsed_IsFound()
    {
        await Serializer.ToStreamAsync(Stream.Null, 1.5, SerializationContext.Default, CancellationToken.None);
        Assert.Null(SerializationContext.Default.FindTypeSerializer(typeof(TypeRegisteredLate)));

        Serializer.RegisterSerializer(new TypeRegisteredLateSerializer());

        Assert.IsType<TypeRegisteredLateSerializer>(SerializationContext.Default.FindTypeSerializer(typeof(TypeRegisteredLate)));
    }

    [Fact]
    public void RegisteringASecondSerializerForTheSameType_IsRefused_AndTheFirstStays()
    {
        var first = new TypeRegisteredTwiceSerializer();
        Serializer.RegisterSerializer(first);

        Assert.Throws<ArgumentException>(() => Serializer.RegisterSerializer(new TypeRegisteredTwiceSerializer()));
        Assert.Same(first, Serializer.GetSerializer<TypeRegisteredTwice>());
    }

    [Fact]
    public async Task WritingManySerializableValues_SetsNothingAsidePerValue()
    {
        var context = new SerializationContext { AllowNullValues = false };
        var values = Enumerable.Range(0, 10 * Count).Select(x => new Tally { Value = x }).ToArray();
        await Serializer.ToStreamAsync(Stream.Null, values, context, CancellationToken.None);

        var before = GC.GetAllocatedBytesForCurrentThread();
        await Serializer.ToStreamAsync(Stream.Null, values, context, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 16 * 1024, $"Writing {values.Length} serializable values set aside {allocated} bytes.");
    }
}
