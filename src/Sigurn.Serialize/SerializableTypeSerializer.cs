
namespace Sigurn.Serialize;

sealed class SerializableTypeSerializer : ITypeSerializer
{
    public SerializableTypeSerializer(Type type)
    {
        Type = type;
    }

    public Type Type { get; }

    public async Task<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (type != Type)
            throw new ArgumentException($"This serializer cannot serialize type '{type}'. It can serialize only instances of type '{Type}'");

        var serializable = (ISerializable?)Activator.CreateInstance(type) ?? 
            throw new SerializationException($"Cannot create an instance of the type '{Type}'");

        await serializable.FromStreamAsync(stream, context, cancellationToken);

        return serializable;
    }

    public Task ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (type != Type)
            throw new ArgumentException($"This serializer cannot serialize type '{type}'. It can serialize only instances of type '{Type}'");

        if (value is not ISerializable serializable)
            throw new ArgumentException("Provided value does not implement ISerializable interface");

        return serializable.ToStreamAsync(stream, context, cancellationToken);        
    }
}