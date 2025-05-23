//HintName: TestClassSerializer.g.cs
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

using Sigurn.Serialize;

namespace MyCode.Serializers;

internal sealed class TestClassSerializer : ITypeSerializer<MyCode.TestClass>
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        Serializer.RegisterSerializer<MyCode.TestClass>(() => new TestClassSerializer(), false);
    }

    private TestClassSerializer()
    {
    }

    public Task ToStreamAsync(Stream stream, MyCode.TestClass value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return Task.CompletedTask;
    }

    public Task<MyCode.TestClass> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(new MyCode.TestClass()
        {
        });
    }
}
