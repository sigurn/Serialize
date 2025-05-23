using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sigurn.Serialize.Tests;

record TestGlobalRecordType();
record TestLocalRecordType();

class GlobalSerializer : ITypeSerializer<TestGlobalRecordType>
{
    public Task<TestGlobalRecordType> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestGlobalRecordType());
    }

    public Task ToStreamAsync(Stream stream, TestGlobalRecordType value, SerializationContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

class LocalSerializer : ITypeSerializer<TestLocalRecordType>
{
    public Task<TestLocalRecordType> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestLocalRecordType());
    }

    public Task ToStreamAsync(Stream stream, TestLocalRecordType value, SerializationContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class RegisterSerializersTests
{
    static RegisterSerializersTests()
    {
        Serializer.RegisterSerializer(() => new GlobalSerializer(), true);
        Serializer.RegisterSerializer(() => new LocalSerializer(), false);
    }

    [Fact]
    public void GlobalSerializerAvailableGlobally()
    {
        var serializer = SerializationContext.Default.FindTypeSerializer(typeof(TestGlobalRecordType));
        Assert.NotNull(serializer);
    }

    [Fact]
    public void GlobalSerializerAvailableLocally()
    {
        var serializer = Serializer.GetSerializer<TestGlobalRecordType>();
        Assert.NotNull(serializer);
    }

    [Fact]
    public void GlobalSerializerGlobalAndLocalInstancesAreTheSame()
    {
        Assert.Same(Serializer.GetSerializer<TestGlobalRecordType>(), SerializationContext.Default.FindTypeSerializer(typeof(TestGlobalRecordType)));
    }

    [Fact]
    public void LocalSerializerNotAvailableGlobally()
    {
        Assert.Null(SerializationContext.Default.FindTypeSerializer(typeof(TestLocalRecordType)));
    }

    [Fact]
    public void LocalSerializerAvailableLocally()
    {
        var serializer = Serializer.GetSerializer<TestLocalRecordType>();
        Assert.NotNull(serializer);
    }
}