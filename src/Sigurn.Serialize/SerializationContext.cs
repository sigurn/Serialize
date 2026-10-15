using System.Text;

namespace Sigurn.Serialize;

public record class SerializationContext
{
    public virtual ITypeSerializer? FindTypeSerializer(Type type)
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

    /// <summary>
    /// How many elements a collection read starts with room for, unless it claims fewer, when <see cref="InitialCollectionCapacity"/> is not set.
    /// </summary>
    public const int DefaultInitialCollectionCapacity = 64 * 1024;

    private int _initialCollectionCapacity = DefaultInitialCollectionCapacity;
    private int? _maxCollectionLength;
    private int? _maxStringBytes;

    /// <summary>
    /// How many elements a collection read starts with room for — or how many bytes a string read does — unless it claims fewer.
    /// </summary>
    /// <remarks>
    /// <para>The room grows as the elements actually arrive, so a collection that claims more elements than its bytes hold is refused
    /// when they run out, having set aside no more than they held. A larger value spares a very large collection the copies its growth
    /// takes; a smaller one sets aside less for a short one.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int InitialCollectionCapacity
    {
        get => _initialCollectionCapacity;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _initialCollectionCapacity = value;
        }
    }

    /// <summary>
    /// The most elements a collection may hold to be written or read in this context, or <see langword="null"/> for no limit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int? MaxCollectionLength
    {
        get => _maxCollectionLength;
        init
        {
            if (value is { } limit)
                ArgumentOutOfRangeException.ThrowIfNegative(limit);
            _maxCollectionLength = value;
        }
    }

    /// <summary>
    /// The most bytes a string may take, in the context's <see cref="Encoding"/>, to be written or read in this context, or
    /// <see langword="null"/> for no limit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int? MaxStringBytes
    {
        get => _maxStringBytes;
        init
        {
            if (value is { } limit)
                ArgumentOutOfRangeException.ThrowIfNegative(limit);
            _maxStringBytes = value;
        }
    }

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