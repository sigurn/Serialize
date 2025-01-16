//HintName: TestStructSerializer.g.cs
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

using Sigurn.Serialize;

namespace MyCode.Serializers;

internal sealed class TestStructSerializer : ITypeSerializer<MyCode.TestStruct>
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        Serializer.RegisterSerializer<MyCode.TestStruct>(() => new TestStructSerializer());
    }

    private TestStructSerializer()
    {
    }

    public async Task ToStreamAsync(Stream stream, MyCode.TestStruct value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        await Serializer.ToStreamAsync<System.DateTime>(stream, value.Prop8, context, cancellationToken);
        await Serializer.ToStreamAsync<string>(stream, value.Prop1, context, cancellationToken);
        await Serializer.ToStreamAsync<int>(stream, value.Prop2, context, cancellationToken);
        await Serializer.ToStreamAsync<System.Collections.Generic.IList<System.Guid>>(stream, value.Prop4, context, cancellationToken);
    }

    public async Task<MyCode.TestStruct> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return new MyCode.TestStruct()
        {
            Prop8 = await Serializer.FromStreamAsync<System.DateTime>(stream, context, cancellationToken),
            Prop1 = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken),
            Prop2 = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken),
            Prop4 = await Serializer.FromStreamAsync<System.Collections.Generic.IList<System.Guid>>(stream, context, cancellationToken),
        };
    }
}
