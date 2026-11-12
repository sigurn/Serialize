namespace Sigurn.Serialize;

// Serializers of closed generic types, built from the static types alone: nothing here turns a Type into code at run time, so they
// work where code cannot be made (Native AOT). Each writes and reads exactly the bytes the reflective serializers do for its type.

delegate ValueTask<object> ReadItems(Stream stream, SerializationContext context, CancellationToken cancellationToken);

sealed class ItemsSerializer<TCollection, TItem>(Func<CollectionsSerializer.Items<TItem>, ReadItems> read) : ITypeSerializer<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly ReadItems _read = read(CollectionsSerializer.Items<TItem>.Instance);

    public ValueTask ToStreamAsync(Stream stream, TCollection value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        return CollectionsSerializer.Items<TItem>.Instance.ToStreamAsync(stream, value, context, cancellationToken);
    }

    public async ValueTask<TCollection> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return (TCollection)await _read(stream, context, cancellationToken).ConfigureAwait(false);
    }
}

sealed class EntriesSerializer<TCollection, TKey, TValue>(Func<CollectionsSerializer.Entries<TKey, TValue>, ReadItems> read) : ITypeSerializer<TCollection>
    where TCollection : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly ReadItems _read = read(CollectionsSerializer.Entries<TKey, TValue>.Instance);

    public ValueTask ToStreamAsync(Stream stream, TCollection value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        return CollectionsSerializer.Entries<TKey, TValue>.Instance.ToStreamAsync(stream, value, context, cancellationToken);
    }

    public async ValueTask<TCollection> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return (TCollection)await _read(stream, context, cancellationToken).ConfigureAwait(false);
    }
}

sealed class KeyValuePairSerializer<TKey, TValue> : ITypeSerializer<KeyValuePair<TKey, TValue>>
{
    public async ValueTask ToStreamAsync(Stream stream, KeyValuePair<TKey, TValue> value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        await Serializer.ToStreamAsync(stream, value.Key, context, cancellationToken).ConfigureAwait(false);
        await Serializer.ToStreamAsync(stream, value.Value, context, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<KeyValuePair<TKey, TValue>> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        var key = await Serializer.FromStreamAsync<TKey>(stream, context, cancellationToken).ConfigureAwait(false);
        var value = await Serializer.FromStreamAsync<TValue>(stream, context, cancellationToken).ConfigureAwait(false);
        return new KeyValuePair<TKey, TValue>(key!, value!);
    }
}

sealed class SerializableTypeSerializer<T> : ITypeSerializer<T>
    where T : ISerializable, new()
{
    public ValueTask ToStreamAsync(Stream stream, T value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(context);

        return value.ToStreamAsync(stream, context, cancellationToken);
    }

    public async ValueTask<T> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        var value = new T();
        await value.FromStreamAsync(stream, context, cancellationToken).ConfigureAwait(false);
        return value;
    }
}
