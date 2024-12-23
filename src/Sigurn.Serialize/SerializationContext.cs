using System.Text;

namespace Sigurn.Serialize;

public record class SerializationContext
{
    public ITypeSerializer? FindTypeSerializer(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_typeSerializers.TryGetValue(type, out ITypeSerializer? serializer))
            return serializer;

        return Serializer.FindTypeSerializer(type);
    }

    public ByteOrder ByteOrder { get; init; } = ByteOrder.BigEndian;

    public UuidForm UuidForm { get; init; } = UuidForm.Linux;

    public bool AllowNullValues { get; init; } = true;

    public Encoding Encoding { get; init; } = Encoding.UTF8;

    private Dictionary<Type, ITypeSerializer> _typeSerializers = [];
    public IReadOnlyList<ITypeSerializer> TypeSerializers
    { 
        get => _typeSerializers.Values.ToArray();
        init =>  _typeSerializers = value.ToDictionary(x => x.Type);
    }

    public static readonly SerializationContext Default = new SerializationContext()
    {
        ByteOrder = ByteOrder.BigEndian,
        UuidForm = UuidForm.Linux,
        Encoding = Encoding.UTF8,
        AllowNullValues = true
    };
}