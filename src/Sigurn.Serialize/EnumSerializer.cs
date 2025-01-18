
using System.Reflection.Metadata;

namespace Sigurn.Serialize;

class EnumSerializer : IGeneralSerializer
{
    public async Task<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (!type.IsEnum)
            throw new ArgumentException($"The enum serializer cannot serialize type '{type}'");

        var value = await Serializer.FromStreamAsync(stream, Enum.GetUnderlyingType(type), context, cancellationToken);
        
        if (value is null)
            throw new SerializationException($"Cannot deserialize null as instance of '{type}' enum.");

        var res = Enum.ToObject(type, value);
        foreach(var v in Enum.GetValues(type))
            if (v.Equals(res)) return res;

        throw new SerializationException($"Unknown value '{res}' for enum '{type}'");
    }

    public bool IsTypeSupported(Type type)
    {
        return type.IsEnum;
    }

    public async Task ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        var underlyingType = Enum.GetUnderlyingType(type);
        await Serializer.ToStreamAsync(stream, underlyingType, Convert.ChangeType(value, underlyingType), context, cancellationToken);
    }
}