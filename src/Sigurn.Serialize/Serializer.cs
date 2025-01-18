namespace Sigurn.Serialize;

/// <summary>
/// This class provides methods for serialization.
/// </summary>
/// <remarks>
/// This class serves as a base and entry point to serialization functionality.
/// </remarks>
public static class Serializer
{
    private static readonly Dictionary<Type, Lazy<ITypeSerializer>> _typeSerializers = [];

    private static readonly List<IGeneralSerializer> _genericSerializers =
    [
        new CollectionsSerializer(),
        new KeyValuePairSerializer(),
        new EnumSerializer()
    ];

    static Serializer()
    {
        RegisterSerializer(() => new StandardTypeSerializer<bool>(1, x => x ? new byte[]{1} : new byte[]{0}, x => x[0] != 0));

        RegisterSerializer(() => new StandardTypeSerializer<byte>(1, x => new byte[]{x}, x => x[0]));
        RegisterSerializer(() => new StandardTypeSerializer<sbyte>(1, x => new byte[]{(byte)x}, x => (sbyte)x[0]));

        RegisterSerializer(() => new StandardTypeSerializer<short>(2, x => BitConverter.GetBytes(x), x => BitConverter.ToInt16(x)));
        RegisterSerializer(() => new StandardTypeSerializer<ushort>(2, x => BitConverter.GetBytes(x), x => BitConverter.ToUInt16(x)));

        RegisterSerializer(() => new StandardTypeSerializer<int>(4, x => BitConverter.GetBytes(x), x => BitConverter.ToInt32(x)));
        RegisterSerializer(() => new StandardTypeSerializer<uint>(4, x => BitConverter.GetBytes(x), x => BitConverter.ToUInt32(x)));

        RegisterSerializer(() => new StandardTypeSerializer<long>(8, x => BitConverter.GetBytes(x), x => BitConverter.ToInt64(x)));
        RegisterSerializer(() => new StandardTypeSerializer<ulong>(8, x => BitConverter.GetBytes(x), x => BitConverter.ToUInt64(x)));

        RegisterSerializer(() => new StandardTypeSerializer<float>(4, x => BitConverter.GetBytes(x), x => BitConverter.ToSingle(x)));
        RegisterSerializer(() => new StandardTypeSerializer<double>(8, x => BitConverter.GetBytes(x), x => BitConverter.ToDouble(x)));

        RegisterSerializer(() => new StandardTypeSerializer<decimal>(16, 
            x => decimal.GetBits(x).SelectMany(x => BitConverter.GetBytes(x)).ToArray(),
            x => new decimal([  BitConverter.ToInt32(x[0..4]), 
                                BitConverter.ToInt32(x[4..8]),
                                BitConverter.ToInt32(x[8..12]),
                                BitConverter.ToInt32(x[12..16])])));

        RegisterSerializer(() => new StandardTypeSerializer<DateTime>(8, 
            x => BitConverter.GetBytes(x.Ticks),
            x => new DateTime(BitConverter.ToInt64(x))));

        RegisterSerializer(() => new StandardTypeSerializer<TimeSpan>(8, 
            x => BitConverter.GetBytes(x.Ticks),
            x => new TimeSpan(BitConverter.ToInt64(x))));

        RegisterSerializer(() => new DateSerializer());
        RegisterSerializer(() => new TimeSerializer());

        RegisterSerializer(() => new StringSerializer());

        RegisterSerializer(() => new GuidSerializer());
        RegisterSerializer(() => new VersionSerializer());
    }

    /// <summary>
    /// Registers global serializer which will be available application wide for all serializations.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializer">Instance of the type serializer.</param>
    public static void RegisterSerializer<T>(ITypeSerializer<T> serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        lock(_typeSerializers)
            _typeSerializers.Add(typeof(T), new Lazy<ITypeSerializer>(serializer));
    }

    /// <summary>
    /// Registers global serializer which will be available application wide for all serializations.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializerFactory">Serializer factory. This factory will be used to create instance on the type serializer.</param>
    /// <remarks>
    /// The serializer will be created on the first use and will stay forever.
    /// </remarks>
    public static void RegisterSerializer<T>(Func<ITypeSerializer<T>> serializerFactory)
    {
        ArgumentNullException.ThrowIfNull(serializerFactory);

        lock(_typeSerializers)
            _typeSerializers.Add(typeof(T), new Lazy<ITypeSerializer>(serializerFactory));
    }

    /// <summary>
    /// Registers global serializer which will be available application wide for all serializations.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializerFactory">Serializer factory. This factory will be used to create instance on the type serializer.</param>
    /// <exception cref="ArgumentException">The exception thrown when there are problems with the arguments.</exception>
    public static void RegisterSerializer<T>(Func<ITypeSerializer> serializerFactory)
    {
        ArgumentNullException.ThrowIfNull(serializerFactory);

        if (typeof(T) == typeof(object))
            throw new ArgumentException("Type serializer cannot be used for serializing objects. Please use GeneralSerializer for that.");

        lock(_typeSerializers)
            _typeSerializers.Add(typeof(T), new Lazy<ITypeSerializer>(serializerFactory));
    }

    /// <summary>
    /// Registers global serializer which will be available application wide for all serializations.
    /// </summary>
    /// <param name="serializer">Type serializer instance.</param>
    /// <exception cref="ArgumentException">The exception thrown when there are problems with the arguments.</exception>
    public static void RegisterSerializer(ITypeSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        if(serializer.Type is null)
            throw new ArgumentException("Serializer must declare what type it can serialize. Type cannot be null", nameof(serializer));

        if (serializer.Type == typeof(object))
            throw new ArgumentException("Type serializer cannot be used for serializing objects. Please use GenericSerializer for that.");

        lock(_typeSerializers)
            _typeSerializers.Add(serializer.Type, new Lazy<ITypeSerializer>(serializer));
    }

    /// <summary>
    /// Registers global serializer which will be available application wide for all serializations.
    /// </summary>
    /// <param name="serializer">General serializer instance.</param>
    /// <exception cref="ArgumentNullException">The exception thrown when there are problems with the arguments.</exception>
    public static void RegisterSerializer(IGeneralSerializer serializer)
    {
        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));
            
        lock(_genericSerializers)
            _genericSerializers.Add(serializer);
    }

    /// <summary>
    /// Finds serializer for the specified type.
    /// </summary>
    /// <param name="type">Type which has to be serialized.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">The exception thrown when there are problems with the arguments.</exception>
    public static ITypeSerializer? FindTypeSerializer(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (type.IsAssignableTo(typeof(ISerializable)))
            return new SerializableTypeSerializer(type);
            
        lock(_typeSerializers)
            if (_typeSerializers.TryGetValue(type, out Lazy<ITypeSerializer>? lazySerializer))
                return lazySerializer.Value;

        ITypeSerializer? serializer = null;
        lock(_genericSerializers)
            serializer = _genericSerializers.Where(x => x.IsTypeSupported(type)).FirstOrDefault();

        if (serializer is not null)
            return serializer;

        return null;
    }

    /// <summary>
    /// Stores provided instance of the type to stream.
    /// </summary>
    /// <typeparam name="T">Type which instance has to be stored in the stream.</typeparam>
    /// <param name="stream">Stream where instance of the type will be stored.</param>
    /// <param name="value">Instance of the type to store in the stream.</param>
    /// <param name="context">Context which defines serialization contexts and serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token which allows to cancel the opertaion.</param>
    /// <returns>Task which can be awaited on.</returns>
    /// <exception cref="SerializationException">It is thrown when there is are some problems with the instance serialization.</exception>
    public async static Task ToStreamAsync<T>(Stream stream, T? value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (context is null)
            context = SerializationContext.Default;

        var st = typeof(T);
        bool isNullable = false;
        var serializer = context.FindTypeSerializer(typeof(T));
        if (serializer is null)
        {
            st = Nullable.GetUnderlyingType(st);
            if (st is null)
                throw new SerializationException($"Cannot find serializer for type '{typeof(T)}'");

            serializer = context.FindTypeSerializer(st);
            if (serializer is null)
                throw new SerializationException($"Cannot find serializer for type neither '{typeof(T)}' nor '{st}'");

            isNullable = true;
        }

        if (!context.AllowNullValues && value is null)
            throw new SerializationException("Cannot serialize null when context prohibits null values");

        if (context.AllowNullValues && (typeof(T).IsClass || typeof(T).IsInterface || isNullable))
            await stream.WriteAsync(new byte[]{ value is not null ? (byte)1 : (byte)0}, cancellationToken).ConfigureAwait(false);

        if (value is null) return;

        if (serializer is ITypeSerializer<T> typeSerializer)
            await typeSerializer.ToStreamAsync(stream, value, context, cancellationToken);
        else
            await serializer.ToStreamAsync(stream, st, value, context, cancellationToken);
    }

    /// <summary>
    /// Stores provided instance of the type to stream.
    /// </summary>
    /// <param name="stream">Stream where instance of the type will be stored.</param>
    /// <param name="type">Type which instance has to be stored in the stream.</param>
    /// <param name="value">Instance of the type to store in the stream.</param>
    /// <param name="context">Context which defines serialization contexts and serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token which allows to cancel the opertaion.</param>
    /// <returns>Task which can be awaited on.</returns>
    /// <exception cref="SerializationException">It is thrown when there is are some problems with the instance serialization.</exception>
    public static async Task ToStreamAsync(Stream stream, Type type, object? value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (context is null)
            context = SerializationContext.Default;

        var st = type;
        bool isNullable = false;
        var serializer = context.FindTypeSerializer(st);
        if (serializer is null)
        {
            st = Nullable.GetUnderlyingType(st);
            if (st is null)
                throw new SerializationException($"Cannot find serializer for type '{type}'");

            serializer = context.FindTypeSerializer(st);
            if (serializer is null)
                throw new SerializationException($"Cannot find serializer for type neither '{type}' nor '{st}'");

            isNullable = true;
        }

        if (!context.AllowNullValues && value is null)
            throw new SerializationException("Cannot serialize null when context prohibits null values");

        if (context.AllowNullValues && (type.IsClass || type.IsInterface || isNullable))
            await stream.WriteAsync(new byte[]{ value is not null ? (byte)1 : (byte)0}, cancellationToken).ConfigureAwait(false);

        if (value is null) return;

        await serializer.ToStreamAsync(stream, st, value, context, cancellationToken);
    }

    /// <summary>
    /// Stores an instance of the type to from the stream.
    /// </summary>
    /// <typeparam name="T">Type which which has to be loaded from the stream.</typeparam>
    /// <param name="stream">Stream where instance of the type will be loaded from.</param>
    /// <param name="context">Context which defines serialization contexts and serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token which allows to cancel the opertaion.</param>
    /// <returns>Task which can be awaited on and where the loaded instance of the type can be aquired from.</returns>
    /// <exception cref="SerializationException">It is thrown when there is are some problems with the instance serialization.</exception>
    public async static Task<T?> FromStreamAsync<T>(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (context is null)
            context = SerializationContext.Default;

        var st = typeof(T);
        bool isNullable = false;
        
        var serializer = FindTypeSerializer(st);
        if (serializer is null)
        {
            st = Nullable.GetUnderlyingType(st);
            if (st is null)
                throw new SerializationException($"Cannot find serializer for type '{typeof(T)}'");

            serializer = FindTypeSerializer(st) ?? 
                throw new SerializationException($"Cannot find serializer for type neither '{typeof(T)}' nor '{st}'");

            isNullable = true;
        }

        if (context.AllowNullValues && (typeof(T).IsClass || typeof(T).IsInterface || isNullable))
        {
            var buf = new byte[1];
            var len = await stream.ReadAsync(buf, cancellationToken);
            if (len != 1)
                throw new SerializationException($"Failed to read neccessary data from stream");

            if (buf[0] == 0) return default;
        }

        return (T)await serializer.FromStreamAsync(stream, st, context, cancellationToken);
    }

    /// <summary>
    /// Stores an instance of the type to from the stream.
    /// </summary>
    /// <param name="stream">Stream where instance of the type will be loaded from.</param>
    /// <param name="type">Type which which has to be loaded from the stream.</param>
    /// <param name="context">Context which defines serialization contexts and serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token which allows to cancel the opertaion.</param>
    /// <returns>Task which can be awaited on and where the loaded instance of the type can be aquired from.</returns>
    /// <exception cref="SerializationException">It is thrown when there is are some problems with the instance serialization.</exception>
    public async static Task<object?> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        if (context is null)
            context = SerializationContext.Default;

        var st = type;
        bool isNullable = false;

        var serializer = FindTypeSerializer(st);
        if (serializer is null)
        {
            st = Nullable.GetUnderlyingType(st);
            if (st is null)
                throw new SerializationException($"Cannot find serializer for type '{type}'");

            serializer = FindTypeSerializer(st) ?? 
                throw new SerializationException($"Cannot find serializer for type neither '{type}' nor '{st}'");

            isNullable = true;
        }

        if (context.AllowNullValues && (type.IsClass || type.IsInterface || isNullable))
        {
            var buf = new byte[1];
            var len = await stream.ReadAsync(buf, cancellationToken);
            if (len != 1)
                throw new SerializationException($"Failed to read neccessary data from stream");

            if (buf[0] == 0) return default;
        }

        return await serializer.FromStreamAsync(stream, st, context, cancellationToken);
   }
}