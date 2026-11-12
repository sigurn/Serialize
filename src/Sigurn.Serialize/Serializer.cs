using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

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

    // Serializers of closed generic types registered by the Register* helpers — mostly by generated code. They are looked up after the
    // serializers registered with RegisterSerializer, never refuse a later RegisterSerializer for the same type and are not seen by GetSerializer.
    private static FrozenDictionary<Type, Lazy<ITypeSerializer>> _autoSerializers = FrozenDictionary<Type, Lazy<ITypeSerializer>>.Empty;

    // Whether the reflective serializers (collections, key-value pairs, ISerializable) may be used for a type nothing registered. They make code
    // at run time, so where that cannot be done (Native AOT) the trimmer drops them and such a type is refused with a hint what to register.
    internal static bool ReflectionFallbackDisabled { get; set; }

    private static bool UseReflectionFallback => RuntimeFeature.IsDynamicCodeSupported && !ReflectionFallbackDisabled;

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

    private static void RegisterAuto(Type type, Func<ITypeSerializer> serializerFactory)
    {
        if (Volatile.Read(ref _autoSerializers).ContainsKey(type))
            return;

        lock (_registration)
        {
            if (!_autoSerializers.ContainsKey(type))
                Volatile.Write(ref _autoSerializers, With(_autoSerializers, type, new Lazy<ITypeSerializer>(serializerFactory)));
        }
    }

    private static void RegisterItems<TCollection, TItem>(Func<CollectionsSerializer.Items<TItem>, ReadItems> read)
        where TCollection : IEnumerable<TItem> =>
        RegisterAuto(typeof(TCollection), () => new ItemsSerializer<TCollection, TItem>(read));

    private static void RegisterEntries<TCollection, TKey, TValue>(Func<CollectionsSerializer.Entries<TKey, TValue>, ReadItems> read)
        where TCollection : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull =>
        RegisterAuto(typeof(TCollection), () => new EntriesSerializer<TCollection, TKey, TValue>(read));

    /// <summary>Makes arrays of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks>
    /// <para>The Register* helpers make a closed generic type serializable without making code at run time. The serializers generated for
    /// <see cref="GenerateSerializerAttribute"/> call them for the types of their members, so they are needed only for types used otherwise.</para>
    /// <para>A serializer registered with <c>RegisterSerializer</c> for the same type takes precedence, and registering one twice does nothing.</para>
    /// </remarks>
    public static void RegisterArray<T>() =>
        RegisterItems<T[], T>(static items => items.ArrayFromStreamAsync);

    /// <summary>Makes <see cref="List{T}"/>, <see cref="IList{T}"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyList{T}"/> and
    /// <see cref="IReadOnlyCollection{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterList<T>()
    {
        RegisterItems<List<T>, T>(static items => items.ListFromStreamAsync);
        RegisterItems<IList<T>, T>(static items => items.ListFromStreamAsync);
        RegisterItems<ICollection<T>, T>(static items => items.ListFromStreamAsync);
        RegisterItems<IReadOnlyList<T>, T>(static items => items.ListFromStreamAsync);
        RegisterItems<IReadOnlyCollection<T>, T>(static items => items.ListFromStreamAsync);
    }

    /// <summary>Makes <see cref="HashSet{T}"/>, <see cref="ISet{T}"/> and <see cref="IReadOnlySet{T}"/> of <typeparamref name="T"/>
    /// serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterHashSet<T>()
    {
        RegisterItems<HashSet<T>, T>(static items => items.HashSetFromStreamAsync);
        RegisterItems<ISet<T>, T>(static items => items.HashSetFromStreamAsync);
        RegisterItems<IReadOnlySet<T>, T>(static items => items.HashSetFromStreamAsync);
    }

    /// <summary>Makes <see cref="Stack{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterStack<T>() =>
        RegisterItems<Stack<T>, T>(static items => items.StackFromStreamAsync);

    /// <summary>Makes <see cref="Queue{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterQueue<T>() =>
        RegisterItems<Queue<T>, T>(static items => items.QueueFromStreamAsync);

    /// <summary>Makes <see cref="SortedSet{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterSortedSet<T>() =>
        RegisterItems<SortedSet<T>, T>(static items => items.SortedSetFromStreamAsync);

    /// <summary>Makes <see cref="LinkedList{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterLinkedList<T>() =>
        RegisterItems<LinkedList<T>, T>(static items => items.LinkedListFromStreamAsync);

    /// <summary>Makes <see cref="ImmutableArray{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterImmutableArray<T>() =>
        RegisterItems<ImmutableArray<T>, T>(static items => items.ImmutableArrayFromStreamAsync);

    /// <summary>Makes <see cref="ImmutableList{T}"/> and <see cref="IImmutableList{T}"/> of <typeparamref name="T"/> serializable without
    /// reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterImmutableList<T>()
    {
        RegisterItems<ImmutableList<T>, T>(static items => items.ImmutableListFromStreamAsync);
        RegisterItems<IImmutableList<T>, T>(static items => items.ImmutableListFromStreamAsync);
    }

    /// <summary>Makes <see cref="ImmutableSortedSet{T}"/> of <typeparamref name="T"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterImmutableSortedSet<T>() =>
        RegisterItems<ImmutableSortedSet<T>, T>(static items => items.ImmutableSortedSetFromStreamAsync);

    /// <summary>Makes <see cref="Dictionary{TKey, TValue}"/> and <see cref="IReadOnlyDictionary{TKey, TValue}"/> serializable without
    /// reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterDictionary<TKey, TValue>() where TKey : notnull
    {
        RegisterEntries<Dictionary<TKey, TValue>, TKey, TValue>(static entries => entries.DictionaryFromStreamAsync);
        RegisterEntries<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>(static entries => entries.DictionaryFromStreamAsync);
    }

    /// <summary>Makes <see cref="SortedDictionary{TKey, TValue}"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterSortedDictionary<TKey, TValue>() where TKey : notnull =>
        RegisterEntries<SortedDictionary<TKey, TValue>, TKey, TValue>(static entries => entries.SortedDictionaryFromStreamAsync);

    /// <summary>Makes <see cref="SortedList{TKey, TValue}"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterSortedList<TKey, TValue>() where TKey : notnull =>
        RegisterEntries<SortedList<TKey, TValue>, TKey, TValue>(static entries => entries.SortedListFromStreamAsync);

    /// <summary>Makes <see cref="ImmutableSortedDictionary{TKey, TValue}"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterImmutableSortedDictionary<TKey, TValue>() where TKey : notnull =>
        RegisterEntries<ImmutableSortedDictionary<TKey, TValue>, TKey, TValue>(static entries => entries.ImmutableSortedDictionaryFromStreamAsync);

    /// <summary>Makes <see cref="KeyValuePair{TKey, TValue}"/> serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterKeyValuePair<TKey, TValue>() =>
        RegisterAuto(typeof(KeyValuePair<TKey, TValue>), static () => new KeyValuePairSerializer<TKey, TValue>());

    /// <summary>Makes <typeparamref name="T"/>, which implements <see cref="ISerializable"/>, serializable without reflection (Native AOT).</summary>
    /// <remarks><inheritdoc cref="RegisterArray{T}" path="/remarks"/></remarks>
    public static void RegisterSerializable<T>() where T : ISerializable, new() =>
        RegisterAuto(typeof(T), static () => new SerializableTypeSerializer<T>());

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
        {
            if (Volatile.Read(ref _autoSerializers).TryGetValue(type, out var registered))
                return registered.Value;

            if (UseReflectionFallback)
                return _serializableTypeSerializers.GetOrAdd(type, static type => new SerializableTypeSerializer(type));

            // Without reflection a type the generator could not register (no parameterless constructor) may still have a serializer
            // registered with RegisterSerializer, so the lookup goes on.
        }

        if (Volatile.Read(ref _globalTypeSerializers).TryGetValue(type, out Lazy<ITypeSerializer>? lazySerializer))
            return lazySerializer.Value;

        if (Volatile.Read(ref _autoSerializers).TryGetValue(type, out lazySerializer))
            return lazySerializer.Value;

        // A loop, not a query: a lambda capturing the type would be set aside on every lookup, found or not. Without reflection the built-in
        // serializers of collections and key-value pairs are passed over; enums need none, as they go through their underlying type.
        var useReflection = UseReflectionFallback;
        foreach (var serializer in Volatile.Read(ref _genericSerializers))
        {
            if (!useReflection && serializer is CollectionsSerializer or KeyValuePairSerializer)
                continue;

            if (serializer.IsTypeSupported(type))
                return serializer;
        }

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
                throw new SerializationException($"Cannot find serializer for type '{type}'{RegistrationHint(type)}");

            serializer = context.FindTypeSerializer(st) ??
                throw new SerializationException($"Cannot find serializer for type neither '{type}' nor '{st}'{RegistrationHint(st)}");

            isNullable = true;
        }

        return new Resolved(serializer, st, context.AllowNullValues && (type.IsClass || type.IsInterface || isNullable));
    }

    // Where reflection is not used, a generic type nothing registered is refused with what would make it serializable.
    private static string RegistrationHint(Type type) =>
        UseReflectionFallback
            ? string.Empty
            : $". Reflection is not available (Native AOT): mark the type that holds it with [GenerateSerializer] or register it, e.g. with Serializer.RegisterList<T>(), RegisterDictionary<TKey, TValue>() or RegisterSerializable<T>().";

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