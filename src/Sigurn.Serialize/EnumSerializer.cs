namespace Sigurn.Serialize;

class EnumSerializer : IGeneralSerializer
{
    public async ValueTask<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsEnum)
            throw new ArgumentException($"The enum serializer cannot serialize type '{type}'");

        var value = await Serializer.FromStreamAsync(stream, Enum.GetUnderlyingType(type), context, cancellationToken);
        
        if (value is null)
            throw new SerializationException($"Cannot deserialize null as instance of '{type}' enum.");

        // Compared as the underlying values: no array of the enum type has to be made, so this holds where code cannot be made at run time.
        foreach (var v in Enum.GetValuesAsUnderlyingType(type))
            if (v.Equals(value)) return Enum.ToObject(type, value);

        var res = Enum.ToObject(type, value);

        throw new SerializationException($"Unknown value '{res}' for enum '{type}'");
    }

    public bool IsTypeSupported(Type type)
    {
        return type.IsEnum;
    }

    public async ValueTask ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        var underlyingType = Enum.GetUnderlyingType(type);
        await Serializer.ToStreamAsync(stream, underlyingType, Convert.ChangeType(value, underlyingType), context, cancellationToken);
    }
}