using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Sigurn.Serialize;

class KeyValuePairSerializer : IGeneralSerializer
{
    public bool IsTypeSupported(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }
    
    public async ValueTask<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsTypeSupported(type))
            throw new SerializationException($"This serializer does not support type {type}");

        return await Of(type).FromStreamAsync(stream, type, context, cancellationToken);
    }

    private static readonly ConcurrentDictionary<Type, ITypeSerializer> _ofType = new();

    // Only the reflective fallback goes from a Type to its serializer: Serializer consults it only where code can be made at run time.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reached only through the reflective fallback, which Serializer uses only when RuntimeFeature.IsDynamicCodeSupported.")]
    private static ITypeSerializer Of(Type type) =>
        _ofType.GetOrAdd(type, static type =>
            (ITypeSerializer)Activator.CreateInstance(typeof(KeyValuePairSerializer<,>).MakeGenericType(type.GetGenericArguments()))!);

    public async ValueTask ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsTypeSupported(type))
            throw new SerializationException($"This serializer does not support type {type}");

        await Of(type).ToStreamAsync(stream, type, value, context, cancellationToken);
    }
}