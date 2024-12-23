//HintName: TestRecordSerializer.g.cs
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

using Sigurn.Serialize;

namespace MyCode.Serializers;

internal sealed class TestRecordSerializer : ITypeSerializer<MyCode.TestRecord>
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        Serializer.RegisterSerializer<MyCode.TestRecord>(() => new TestRecordSerializer());
    }

    private TestRecordSerializer()
    {
    }

    public async Task ToStreamAsync(Stream stream, MyCode.TestRecord value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        await Serializer.ToStreamAsync<string>(stream, value.Prop1, context, cancellationToken);
        await Serializer.ToStreamAsync<int>(stream, value.Prop2, context, cancellationToken);
        await Serializer.ToStreamAsync<System.Collections.Generic.IList<System.Guid>>(stream, value.Prop4, context, cancellationToken);
        await Serializer.ToStreamAsync<System.DateTime>(stream, value.Prop8, context, cancellationToken);
    }

    public async Task<MyCode.TestRecord> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        return new MyCode.TestRecord()
        {
            Prop1 = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken),
            Prop2 = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken),
            Prop4 = await Serializer.FromStreamAsync<System.Collections.Generic.IList<System.Guid>>(stream, context, cancellationToken),
            Prop8 = await Serializer.FromStreamAsync<System.DateTime>(stream, context, cancellationToken),
        };
    }
}
