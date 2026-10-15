//HintName: EmptyClassSerializer.g.cs
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

using Sigurn.Serialize;

namespace MyCode.Serializers;

internal sealed class EmptyClassSerializer : ITypeSerializer<MyCode.EmptyClass>
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        Serializer.RegisterSerializer<MyCode.EmptyClass>(() => new EmptyClassSerializer(), true);
    }

    private EmptyClassSerializer()
    {
    }

    public ValueTask ToStreamAsync(Stream stream, MyCode.EmptyClass value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.CompletedTask;
    }

    public ValueTask<MyCode.EmptyClass> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.FromResult(new MyCode.EmptyClass()
        {
        });
    }
}
