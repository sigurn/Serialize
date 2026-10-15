using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Sigurn.Serialize;

/// <summary>
/// This class provides methods for serialization.
/// </summary>
/// <remarks>
/// This class serves as a base and entry point to serialization functionality.
/// </remarks>
public static class Serializer
{
    // Serializers are registered seldom and looked up for every value written or read. So a registration builds new tables under
    // the one lock and publishes them whole, and a lookup reads whichever tables are published, without a lock.
    private static readonly object _registration = new();

    private static FrozenDictionary<Type, Lazy<ITypeSerializer>> _typeSerializers = FrozenDictionary<Type, Lazy<ITypeSerializer>>.Empty;
    private static FrozenDictionary<Type, Lazy<ITypeSerializer>> _globalTypeSerializers = FrozenDictionary<Type, Lazy<ITypeSerializer>>.Empty;

    private static IGeneralSerializer[] _genericSerializers =
    [
        new CollectionsSerializer(),
        new KeyValuePairSerializer(),
        new EnumSerializer()
    ];

    private static readonly ConcurrentDictionary<Type, SerializableTypeSerializer> _serializableTypeSerializers = new();

    static Serializer()
    {
        RegisterSerializer(() => new StandardTypeSerializer<bool>(1, (x, b) => b[0] = x ? (byte)1 : (byte)0, x => x[0] != 0));

        RegisterSerializer(() => new StandardTypeSerializer<byte>(1, (x, b) => b[0] = x, x => x[0]));
        RegisterSerializer(() => new StandardTypeSerializer<sbyte>(1, (x, b) => b[0] = (byte)x, x => (sbyte)x[0]));

        RegisterSerializer(() => new StandardTypeSerializer<short>(2, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToInt16(x)));
        RegisterSerializer(() => new StandardTypeSerializer<ushort>(2, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToUInt16(x)));

        RegisterSerializer(() => new StandardTypeSerializer<int>(4, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToInt32(x)));
        RegisterSerializer(() => new StandardTypeSerializer<uint>(4, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToUInt32(x)));

        RegisterSerializer(() => new StandardTypeSerializer<long>(8, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToInt64(x)));
        RegisterSerializer(() => new StandardTypeSerializer<ulong>(8, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToUInt64(x)));

        RegisterSerializer(() => new StandardTypeSerializer<float>(4, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToSingle(x)));
        RegisterSerializer(() => new StandardTypeSerializer<double>(8, (x, b) => BitConverter.TryWriteBytes(b, x), x => BitConverter.ToDouble(x)));

        RegisterSerializer(() => new StandardTypeSerializer<decimal>(16, WriteDecimal, ReadDecimal));

        RegisterSerializer(() => new StandardTypeSerializer<DateTime>(8,
            (x, b) => BitConverter.TryWriteBytes(b, x.Ticks),
            x => new DateTime(BitConverter.ToInt64(x))));

        RegisterSerializer(() => new StandardTypeSerializer<TimeSpan>(8,
            (x, b) => BitConverter.TryWriteBytes(b, x.Ticks),
            x => new TimeSpan(BitConverter.ToInt64(x))));

        RegisterSerializer(() => new DateSerializer());
        RegisterSerializer(() => new TimeSerializer());

        RegisterSerializer(() => new StringSerializer());

        RegisterSerializer(() => new GuidSerializer());
        RegisterSerializer(() => new VersionSerializer());
    }

    private static void WriteDecimal(decimal value, Span<byte> bytes)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        for (int i = 0; i < bits.Length; i++)
            BitConverter.TryWriteBytes(bytes[(i * 4)..], bits[i]);
    }

    private static decimal ReadDecimal(ReadOnlySpan<byte> bytes)
    {
        Span<int> bits = stackalloc int[4];
        for (int i = 0; i < bits.Length; i++)
            bits[i] = BitConverter.ToInt32(bytes.Slice(i * 4, 4));
        return new decimal(bits);
    }

    /// <summary>
    /// Registers serializer.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializer">Instance of the type serializer.</param>
    /// <param name="useGlobally">Defines if the serializer is available application wide for all serializations.</param>
    public static void RegisterSerializer<T>(ITypeSerializer<T> serializer, bool useGlobally = true)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        Register(typeof(T), new Lazy<ITypeSerializer>(serializer), useGlobally);
    }

    /// <summary>
    /// Registers serializer.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializerFactory">Serializer factory. This factory will be used to create instance on the type serializer.</param>
    /// <param name="useGlobally">Defines if the serializer is available application wide for all serializations.</param>
    /// <remarks>
    /// The serializer will be created on the first use and will stay forever.
    /// </remarks>
    public static void RegisterSerializer<T>(Func<ITypeSerializer<T>> serializerFactory, bool useGlobally = true)
    {
        ArgumentNullException.ThrowIfNull(serializerFactory);

        Register(typeof(T), new Lazy<ITypeSerializer>(serializerFactory), useGlobally);
    }

    /// <summary>
    /// Registers serializer.
    /// </summary>
    /// <typeparam name="T">Type for which the serializer will be used.</typeparam>
    /// <param name="serializerFactory">Serializer factory. This factory will be used to create instance on the type serializer.</param>
    /// <param name="useGlobally">Defines if the serializer is available application wide for all serializations.</param>
    /// <exception cref="ArgumentException">The exception thrown when there are problems with the arguments.</exception>
    public static void RegisterSerializer<T>(Func<ITypeSerializer> serializerFactory, bool useGlobally = true)
    {
        ArgumentNullException.ThrowIfNull(serializerFactory);

        if (typeof(T) == typeof(object))
            throw new ArgumentException("Type serializer cannot be used for serializing objects. Please use GeneralSerializer for that.");

        Register(typeof(T), new Lazy<ITypeSerializer>(serializerFactory), useGlobally);
    }

    /// <summary>
    /// Registers serializer.
    /// </summary>
    /// <param name="serializer">Type serializer instance.</param>
    /// <param name="useGlobally">Defines if the serializer is available application wide for all serializations.</param>
    /// <exception cref="ArgumentException">The exception thrown when there are problems with the arguments.</exception>
    public static void RegisterSerializer(ITypeSerializer serializer, bool useGlobally = true)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        if(serializer.Type is null)
            throw new ArgumentException("Serializer must declare what type it can serialize. Type cannot be null", nameof(serializer));

        if (serializer.Type == typeof(object))
            throw new ArgumentException("Type serializer cannot be used for serializing objects. Please use GeneralSerializer for that.");

        Register(serializer.Type, new Lazy<ITypeSerializer>(serializer), useGlobally);
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

        lock (_registration)
            Volatile.Write(ref _genericSerializers, [.. _genericSerializers, serializer]);
    }

    // Both tables or neither: a type already registered is refused before either table changes.
    private static void Register(Type type, Lazy<ITypeSerializer> serializer, bool useGlobally)
    {
        lock (_registration)
        {
            if (_typeSerializers.ContainsKey(type))
                throw new ArgumentException($"A serializer for the type '{type}' is already registered.");

            Volatile.Write(ref _typeSerializers, With(_typeSerializers, type, serializer));
            if (useGlobally)
                Volatile.Write(ref _globalTypeSerializers, With(_globalTypeSerializers, type, serializer));
        }
    }

    private static FrozenDictionary<Type, Lazy<ITypeSerializer>> With(FrozenDictionary<Type, Lazy<ITypeSerializer>> table, Type type, Lazy<ITypeSerializer> serializer) =>
        table.Append(new KeyValuePair<Type, Lazy<ITypeSerializer>>(type, serializer)).ToFrozenDictionary();

    /// <summary>
    /// Creates serializer instance for the specified type.
    /// </summary>
    /// <typeparam name="T">Type for that serializer should be created.</typeparam>
    /// <returns>Serilizer instance.</returns>
    /// <exception cref="ArgumentException">The exception thrown if serializer for the specified type is not registered.</exception>
    /// <remarks>This method allows to get serializer instance which was registered but is not used globally for serialization.
    /// This allows to register serializers for types but use them in certain serialization context.
    /// </remarks>
    public static ITypeSerializer<T> GetSerializer<T>()
    {
        if (Volatile.Read(ref _typeSerializers).TryGetValue(typeof(T), out var factory))
            return (ITypeSerializer<T>)factory.Value;

        throw new ArgumentException($"Serializer for the type '{typeof(T)}' is not registered.");
    }

    /// <summary>
    /// Finds serializer for the specified type.
    /// </summary>
    /// <param name="type">Type which has to be serialized.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">The exception thrown when there are problems with the arguments.</exception>
    internal static ITypeSerializer? FindTypeSerializer(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (type.IsAssignableTo(typeof(ISerializable)))
            return _serializableTypeSerializers.GetOrAdd(type, static type => new SerializableTypeSerializer(type));

        if (Volatile.Read(ref _globalTypeSerializers).TryGetValue(type, out Lazy<ITypeSerializer>? lazySerializer))
            return lazySerializer.Value;

        // A loop, not a query: a lambda capturing the type would be set aside on every lookup, found or not.
        foreach (var serializer in Volatile.Read(ref _genericSerializers))
            if (serializer.IsTypeSupported(type))
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
    public async static ValueTask ToStreamAsync<T>(Stream stream, T? value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (context is null)
            context = SerializationContext.Default;

        await WriteResolvedAsync(stream, value, Resolve(typeof(T), context), context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// How a value of one type is written and read in one context: the serializer found for it, the type that serializer is handed,
    /// and whether a byte marks the value present. Found once, it serves every value of the type — every element of a collection.
    /// </summary>
    internal readonly record struct Resolved(ITypeSerializer Serializer, Type SerializedType, bool MarksPresence);

    internal static Resolved Resolve(Type type, SerializationContext context)
    {
        var st = type;
        bool isNullable = false;
        var serializer = context.FindTypeSerializer(st);
        if (serializer is null)
        {
            st = Nullable.GetUnderlyingType(st);
            if (st is null)
                throw new SerializationException($"Cannot find serializer for type '{type}'");

            serializer = context.FindTypeSerializer(st) ??
                throw new SerializationException($"Cannot find serializer for type neither '{type}' nor '{st}'");

            isNullable = true;
        }

        return new Resolved(serializer, st, context.AllowNullValues && (type.IsClass || type.IsInterface || isNullable));
    }

    internal static async ValueTask WriteResolvedAsync<T>(Stream stream, T? value, Resolved resolved, SerializationContext context, CancellationToken cancellationToken)
    {
        if (!context.AllowNullValues && value is null)
            throw new SerializationException("Cannot serialize null when context prohibits null values");

        if (resolved.MarksPresence)
            await StreamBytes.WritePresenceAsync(stream, value is not null, cancellationToken).ConfigureAwait(false);

        if (value is null) return;

        if (resolved.Serializer is ITypeSerializer<T> typeSerializer)
            await typeSerializer.ToStreamAsync(stream, value, context, cancellationToken).ConfigureAwait(false);
        else
            await resolved.Serializer.ToStreamAsync(stream, resolved.SerializedType, value, context, cancellationToken).ConfigureAwait(false);
    }

    internal static async ValueTask<T?> ReadResolvedAsync<T>(Stream stream, Resolved resolved, SerializationContext context, CancellationToken cancellationToken)
    {
        if (resolved.MarksPresence && !await StreamBytes.ReadPresenceAsync(stream, cancellationToken).ConfigureAwait(false))
            return default;

        if (resolved.Serializer is ITypeSerializer<T> typeSerializer)
            return await typeSerializer.FromStreamAsync(stream, context, cancellationToken).ConfigureAwait(false);

        return (T)await resolved.Serializer.FromStreamAsync(stream, resolved.SerializedType, context, cancellationToken).ConfigureAwait(false);
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
    public static async ValueTask ToStreamAsync(Stream stream, Type type, object? value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        if (context is null)
            context = SerializationContext.Default;

        var resolved = Resolve(type, context);

        if (!context.AllowNullValues && value is null)
            throw new SerializationException("Cannot serialize null when context prohibits null values");

        if (resolved.MarksPresence)
            await StreamBytes.WritePresenceAsync(stream, value is not null, cancellationToken).ConfigureAwait(false);

        if (value is null) return;

        await resolved.Serializer.ToStreamAsync(stream, resolved.SerializedType, value, context, cancellationToken).ConfigureAwait(false);
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
    public async static ValueTask<T?> FromStreamAsync<T>(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (context is null)
            context = SerializationContext.Default;

        return await ReadResolvedAsync<T>(stream, Resolve(typeof(T), context), context, cancellationToken).ConfigureAwait(false);
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
    public async static ValueTask<object?> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        if (context is null)
            context = SerializationContext.Default;

        var resolved = Resolve(type, context);

        if (resolved.MarksPresence && !await StreamBytes.ReadPresenceAsync(stream, cancellationToken).ConfigureAwait(false))
            return default;

        return await resolved.Serializer.FromStreamAsync(stream, resolved.SerializedType, context, cancellationToken).ConfigureAwait(false);
   }
}