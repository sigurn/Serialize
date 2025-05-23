namespace Sigurn.Serialize.IntegrationTests;

[GenerateSerializer(true)]
public class TestClass
{
    public int Prop1 { get; set; }
    public bool Prop2 { get; set; }
    public IReadOnlyList<int> Prop3 { get; init; } = new List<int>();
}

public class SerializeIntegrationTests
{
    [Fact]
    public async Task CheckGenerator()
    {
        var item = new TestClass
        {
            Prop1 = 100,
            Prop2 = true,
            Prop3 = [1,2,3,4,5,6,7,8,9,0],
        };

        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, item.GetType(), item, SerializationContext.Default, CancellationToken.None);

        stream.Seek(0, SeekOrigin.Begin);

        var item2 = await Serializer.FromStreamAsync<TestClass>(stream, SerializationContext.Default, CancellationToken.None);
        
        Assert.NotNull(item2);
        Assert.Equal(item.Prop1, item2.Prop1);
        Assert.Equal(item.Prop2, item.Prop2);
        Assert.Equal(item.Prop3, item2.Prop3);
    }
}