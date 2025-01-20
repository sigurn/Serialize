
using System.Collections.Concurrent;

namespace Sigurn.Serialize.Tests;


class CustomType
{

}

class CustomTypeSerializer : ITypeSerializer<CustomType>
{
    private readonly ConcurrentBag<string> _log = new ();
    public Task<CustomType> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        _log.Add("FromStreamAsync");
        return Task.FromResult(new CustomType());
    }

    public Task ToStreamAsync(Stream stream, CustomType value, SerializationContext context, CancellationToken cancellationToken)
    {
        _log.Add("ToStreamAsync");
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetLog()
    {
        return _log.ToList();
    }
}

public class ContextSerializersTests
{
    [Fact]
    public async Task CanSerializeWithCustomSerializer()
    {
        var typeSerializer = new CustomTypeSerializer();
        var context = SerializationContext.Default with
        {
            TypeSerializers = [ typeSerializer ]
        };

        using var stream = new MemoryStream();
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await Serializer.ToStreamAsync(stream, new CustomType(), context, cts.Token);
        Assert.Equal(["ToStreamAsync"], typeSerializer.GetLog());
        
        await Serializer.ToStreamAsync(stream, typeof(CustomType), new CustomType(), context, cts.Token);
        Assert.Equal(["ToStreamAsync", "ToStreamAsync"], typeSerializer.GetLog());

        Assert.Equal([1, 1], stream.ToArray());
    }

    [Fact]
    public async Task CanDeserializeWithCustomSerializer()
    {
        var typeSerializer = new CustomTypeSerializer();
        var context = SerializationContext.Default with
        {
            TypeSerializers = [ typeSerializer ]
        };

        using var stream = new MemoryStream([1, 1]);
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var ct = await Serializer.FromStreamAsync<CustomType>(stream, context, cts.Token);
        Assert.NotNull(ct);
        Assert.Equal(["FromStreamAsync"], typeSerializer.GetLog());

        ct = await Serializer.FromStreamAsync(stream, typeof(CustomType), context, cts.Token) as CustomType;
        Assert.NotNull(ct);
        Assert.Equal(["FromStreamAsync", "FromStreamAsync"], typeSerializer.GetLog());
    }
}