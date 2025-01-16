
namespace Sigurn.Serialize;

class KeyValuePairSerializer : IGeneralSerializer
{
    public bool IsTypeSupported(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }


    public async Task<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (!IsTypeSupported(type))
            throw new SerializationException($"This serializer does not support type {type}");

        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];

        var ctor = type.GetConstructor([keyType, valueType]) ?? 
            throw new SerializationException($"Cannot find required constructor for type {type}");

        var key = await Serializer.FromStreamAsync(stream, keyType, context, cancellationToken);
        var value = await Serializer.FromStreamAsync(stream, valueType, context, cancellationToken);

        return ctor.Invoke([key, value]);
    }

    public async Task ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (!IsTypeSupported(type))
            throw new SerializationException($"This serializer does not support type {type}");

        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];

        var keyProp = type.GetProperty("Key") ?? 
            throw new SerializationException($"Cannot get Key property from type {type}");
            
        var valueProp = type.GetProperty("Value") ?? 
            throw new SerializationException($"Cannot get Value property from type {type}");

        await Serializer.ToStreamAsync(stream, keyType, keyProp.GetValue(value), context, cancellationToken);
        await Serializer.ToStreamAsync(stream, valueType, valueProp.GetValue(value), context, cancellationToken);
    }
}