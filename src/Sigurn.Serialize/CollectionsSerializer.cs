
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Sigurn.Serialize;

class CollectionsSerializer : IGeneralSerializer
{
    private static readonly Type[] _genericTypes = [typeof(List<>), typeof(HashSet<>),
        typeof(Stack<>), typeof(Queue<>), typeof(SortedSet<>), typeof(LinkedList<>),
        typeof(IReadOnlyList<>), typeof(IList<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>),
        typeof(ISet<>), typeof(IReadOnlySet<>),
        typeof(ImmutableArray<>), typeof(ImmutableList<>), typeof(IImmutableList<>), typeof(ImmutableSortedSet<>),
        typeof(Dictionary<,>), typeof(SortedDictionary<,>), typeof(SortedList<,>), typeof(IReadOnlyDictionary<,>),
        typeof(ImmutableSortedDictionary<,>)];

    // How many elements a collection holds, as it is written or read: held to the context's limit, if it sets one.
    private static int Counted(int count, SerializationContext context) =>
        context.MaxCollectionLength is { } limit && count > limit
            ? throw new SerializationException($"A collection of {count} elements is longer than the {limit} the context allows.")
            : count;

    private static ValueTask WriteCountAsync(Stream stream, int count, SerializationContext context, CancellationToken cancellationToken) =>
        Serializer.ToStreamAsync(stream, Counted(count, context), context, cancellationToken);

    private static async ValueTask<int> ReadCountAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
        Counted(await Serializer.FromStreamAsync<int>(stream, context, cancellationToken).ConfigureAwait(false), context);

    public bool IsTypeSupported(Type type)
    {
        if (type.IsArray)
            return true;

        if (type.IsGenericType && _genericTypes.Contains(type.GetGenericTypeDefinition()))
            return true;

        return false;
    }

    public async ValueTask<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? throw new SerializationException("Type of array element cannot be null");
            return await ArrayFromStream(stream, elementType, context, cancellationToken);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return await ListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            return await ListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
            return await ListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            return await ListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
            return await ListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            return await HashSetFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISet<>))
            return await HashSetFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlySet<>))
            return await HashSetFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>))
            return await StackFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
            return await QueueFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedSet<>))
            return await SortedSetFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LinkedList<>))
            return await LinkedListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            return await Items.Of(type.GetGenericArguments()[0]).ImmutableArrayFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(ImmutableList<>) || type.GetGenericTypeDefinition() == typeof(IImmutableList<>)))
            return await Items.Of(type.GetGenericArguments()[0]).ImmutableListFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableSortedSet<>))
            return await Items.Of(type.GetGenericArguments()[0]).ImmutableSortedSetFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
            return await Entries.Of(type).DictionaryFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedDictionary<,>))
            return await Entries.Of(type).SortedDictionaryFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedList<,>))
            return await Entries.Of(type).SortedListFromStreamAsync(stream, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableSortedDictionary<,>))
            return await Entries.Of(type).ImmutableSortedDictionaryFromStreamAsync(stream, context, cancellationToken);

        throw new NotImplementedException();
    }

    public async ValueTask ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        
        if (type.IsSZArray)
        {
            var itemType = type.GetElementType() ?? throw new SerializationException("Type of array element cannot be null");
            await Items.Of(itemType).ToStreamAsync(stream, value, context, cancellationToken);
        }
        else if (type.IsArray)
        {
            var itemType = type.GetElementType() ?? throw new SerializationException("Type of array element cannot be null");
            Array array = (Array)value;
            await WriteCountAsync(stream, array.Length, context, cancellationToken);
            foreach(var item in array)
                await Serializer.ToStreamAsync(stream, itemType, item, context, cancellationToken);
        }
        else if (type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
            await Items.Of(type.GetGenericArguments()[0]).ToStreamAsync(stream, value, context, cancellationToken);
        }
        else if (type.IsGenericType && type.GetGenericArguments().Length == 2)
        {
            await Entries.Of(type).ToStreamAsync(stream, value, context, cancellationToken);
        }
        else if (type.IsAssignableTo(typeof(IList)))
        {
            Type? itemType = null;
            if (type.IsGenericType)
                itemType = type.GetGenericArguments()[0];
            IList list = (IList)value;
            await WriteCountAsync(stream, list.Count, context, cancellationToken);
            foreach(var item in list)
                await Serializer.ToStreamAsync(stream, itemType ?? item.GetType(), item, context, cancellationToken);
        }
        else if (type.IsAssignableTo(typeof(IReadOnlyCollection<>)))
        {
            throw new NotImplementedException();
        }
        else if (type.IsAssignableTo(typeof(ICollection)))
        {
            Type? itemType = null;
            if (type.IsGenericType)
                itemType = type.GetGenericArguments()[0];
            ICollection list = (ICollection)value;
            var count = list.Count;
            await WriteCountAsync(stream, count, context, cancellationToken);
            foreach(var item in list)
                await Serializer.ToStreamAsync(stream, itemType ?? item.GetType(), item, context, cancellationToken);
        }
        else if (type.IsAssignableTo(typeof(IEnumerable)))
        {
            Type? itemType = null;
            if (type.IsGenericType && type.GetGenericArguments().Length == 1)
                itemType = type.GetGenericArguments()[0];
            IEnumerable list = (IEnumerable)value;
            int count = 0;
            var enumerator = list.GetEnumerator();
            while(enumerator.MoveNext()) count++;
            await WriteCountAsync(stream, count, context, cancellationToken);
            foreach(var item in list)
                await Serializer.ToStreamAsync(stream, itemType ?? item.GetType(), item, context, cancellationToken);
        }
        else
            throw new SerializationException($"This serializer cannot serialize type {type}");
    }

    private ValueTask<object> ArrayFromStream(Stream stream, Type elementType, SerializationContext context, CancellationToken cancellationToken) =>
        Items.Of(elementType).ArrayFromStreamAsync(stream, context, cancellationToken);

    private ValueTask<object> ListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken) =>
        Items.Of(type.GetGenericArguments()[0]).ListFromStreamAsync(stream, context, cancellationToken);

    // The elements of a collection, written and read as their own type: an element is never boxed on its way to or from the stream,
    // and a large collection sets aside nothing per element beyond what its own serializer does.
    private abstract class Items
    {
        private static readonly ConcurrentDictionary<Type, Items> _ofType = new();

        public static Items Of(Type itemType) =>
            _ofType.GetOrAdd(itemType, static itemType => (Items)Activator.CreateInstance(typeof(Items<>).MakeGenericType(itemType))!);

        public abstract ValueTask ToStreamAsync(Stream stream, object value, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ArrayFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ImmutableArrayFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ImmutableListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ImmutableSortedSetFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);
    }

    // The serializer of the elements is found once for the whole collection, and only when it has an element to find it for: an
    // empty collection of a type no serializer knows is still written and read, as it always was.
    private sealed class Items<TItem> : Items
    {
        public override async ValueTask ToStreamAsync(Stream stream, object value, SerializationContext context, CancellationToken cancellationToken)
        {
            switch (value)
            {
                case ImmutableArray<TItem> { IsDefault: true }:
                    throw new SerializationException($"A default {typeof(ImmutableArray<TItem>)} holds no array to write.");

                case IReadOnlyList<TItem> list:
                    await WriteCountAsync(stream, list.Count, context, cancellationToken).ConfigureAwait(false);
                    if (list.Count == 0)
                        break;

                    var resolved = Serializer.Resolve(typeof(TItem), context);
                    for (int i = 0; i < list.Count; i++)
                        await Serializer.WriteResolvedAsync(stream, list[i], resolved, context, cancellationToken).ConfigureAwait(false);
                    break;

                case IEnumerable<TItem> items:
                    var count = items switch
                    {
                        ICollection<TItem> collection => collection.Count,
                        IReadOnlyCollection<TItem> collection => collection.Count,
                        _ => items.Count(),
                    };
                    await WriteCountAsync(stream, count, context, cancellationToken).ConfigureAwait(false);
                    if (count == 0)
                        break;

                    var resolvedItem = Serializer.Resolve(typeof(TItem), context);
                    foreach (var item in items)
                        await Serializer.WriteResolvedAsync(stream, item, resolvedItem, context, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new SerializationException($"This serializer cannot serialize type {value.GetType()}");
            }
        }

        public override async ValueTask<object> ArrayFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            await ReadArrayAsync(stream, context, cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object> ImmutableArrayFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            ImmutableCollectionsMarshal.AsImmutableArray(await ReadArrayAsync(stream, context, cancellationToken).ConfigureAwait(false));

        public override async ValueTask<object> ImmutableListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            ImmutableList.CreateRange(await ReadArrayAsync(stream, context, cancellationToken).ConfigureAwait(false));

        public override async ValueTask<object> ImmutableSortedSetFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            ImmutableSortedSet.CreateRange(await ReadArrayAsync(stream, context, cancellationToken).ConfigureAwait(false));

        public override async ValueTask<object> ListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            var len = await ReadCountAsync(stream, context, cancellationToken).ConfigureAwait(false);
            if (len <= 0)
                return new List<TItem>();

            var list = new List<TItem>(Math.Min(len, context.InitialCollectionCapacity));

            var resolved = Serializer.Resolve(typeof(TItem), context);
            for (int i = 0; i < len; i++)
                list.Add((await Serializer.ReadResolvedAsync<TItem>(stream, resolved, context, cancellationToken).ConfigureAwait(false))!);
            return list;
        }

        // The room is no more than the context starts a collection with, and grows as the elements arrive, so a count that claims more
        // than follows runs out of bytes having set aside only what they held. The array read is exactly as long as its count.
        private static async ValueTask<TItem[]> ReadArrayAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            var len = await ReadCountAsync(stream, context, cancellationToken).ConfigureAwait(false);
            ArgumentOutOfRangeException.ThrowIfNegative(len);
            if (len == 0)
                return [];

            var array = new TItem[Math.Min(len, context.InitialCollectionCapacity)];
            var resolved = Serializer.Resolve(typeof(TItem), context);
            for (int i = 0; i < len; i++)
            {
                if (i == array.Length)
                    Array.Resize(ref array, (int)Math.Min(len, 2L * array.Length));

                array[i] = (await Serializer.ReadResolvedAsync<TItem>(stream, resolved, context, cancellationToken).ConfigureAwait(false))!;
            }

            return array;
        }
    }

    private async ValueTask<object> HashSetFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(HashSet<>);
        var elementType = type.GetGenericArguments()[0];
        var setType = genericType.MakeGenericType(elementType);
        var ctorArgType = genericType.MakeGenericType(elementType);
        var ctor = setType.GetConstructor([ctorArgType]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        var array = await ArrayFromStream(stream, elementType, context, cancellationToken);
        return ctor.Invoke([array]);
    }

    private async ValueTask<object> StackFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(Stack<>);
        var elementType = type.GetGenericArguments()[0];
        var setType = genericType.MakeGenericType(elementType);
        var ctorArgType = genericType.MakeGenericType(elementType);
        var ctor = setType.GetConstructor([ctorArgType]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        var array = await ArrayFromStream(stream, elementType, context, cancellationToken);
        Array.Reverse((Array)array);
        return ctor.Invoke([array]);
    }

    private async ValueTask<object> QueueFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(Queue<>);
        var elementType = type.GetGenericArguments()[0];
        var setType = genericType.MakeGenericType(elementType);
        var ctorArgType = genericType.MakeGenericType(elementType);
        var ctor = setType.GetConstructor([ctorArgType]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        var array = await ArrayFromStream(stream, elementType, context, cancellationToken);
        return ctor.Invoke([array]);
    }

    private async ValueTask<object> SortedSetFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(SortedSet<>);
        var elementType = type.GetGenericArguments()[0];
        var setType = genericType.MakeGenericType(elementType);
        var ctorArgType = genericType.MakeGenericType(elementType);
        var ctor = setType.GetConstructor([ctorArgType]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        var array = await ArrayFromStream(stream, elementType, context, cancellationToken);
        return ctor.Invoke([array]);
    }

    private async ValueTask<object> LinkedListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(LinkedList<>);
        var elementType = type.GetGenericArguments()[0];
        var setType = genericType.MakeGenericType(elementType);
        var ctorArgType = genericType.MakeGenericType(elementType);
        var ctor = setType.GetConstructor([ctorArgType]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        var array = await ArrayFromStream(stream, elementType, context, cancellationToken);
        return ctor.Invoke([array]);
    }

    // The entries of a dictionary, each key and value written and read as its own type, never boxed, and the serializers of the keys and
    // of the values found once for the whole dictionary — only when it has an entry to find them for.
    private abstract class Entries
    {
        private static readonly ConcurrentDictionary<Type, Entries> _ofType = new();

        public static Entries Of(Type dictionaryType) =>
            _ofType.GetOrAdd(dictionaryType, static dictionaryType =>
                (Entries)Activator.CreateInstance(typeof(Entries<,>).MakeGenericType(dictionaryType.GetGenericArguments()))!);

        public abstract ValueTask ToStreamAsync(Stream stream, object value, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> DictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> SortedDictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> SortedListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

        public abstract ValueTask<object> ImmutableSortedDictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);
    }

    private sealed class Entries<TKey, TValue> : Entries
        where TKey : notnull
    {
        public override async ValueTask ToStreamAsync(Stream stream, object value, SerializationContext context, CancellationToken cancellationToken)
        {
            if (value is not IEnumerable<KeyValuePair<TKey, TValue>> entries)
                throw new SerializationException($"This serializer cannot serialize type {value.GetType()}");

            var count = entries switch
            {
                IReadOnlyCollection<KeyValuePair<TKey, TValue>> collection => collection.Count,
                ICollection<KeyValuePair<TKey, TValue>> collection => collection.Count,
                _ => entries.Count(),
            };
            await WriteCountAsync(stream, count, context, cancellationToken).ConfigureAwait(false);
            if (count == 0)
                return;

            var key = Serializer.Resolve(typeof(TKey), context);
            var item = Serializer.Resolve(typeof(TValue), context);
            foreach (var entry in entries)
            {
                await Serializer.WriteResolvedAsync(stream, entry.Key, key, context, cancellationToken).ConfigureAwait(false);
                await Serializer.WriteResolvedAsync(stream, entry.Value, item, context, cancellationToken).ConfigureAwait(false);
            }
        }

        public override async ValueTask<object> DictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            await ReadAsync(stream, context, static room => new Dictionary<TKey, TValue>(room), static (entries, key, value) => entries.Add(key, value), cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object> SortedDictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            await ReadAsync(stream, context, static _ => new SortedDictionary<TKey, TValue>(), static (entries, key, value) => entries.Add(key, value), cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object> SortedListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            await ReadAsync(stream, context, static room => new SortedList<TKey, TValue>(room), static (entries, key, value) => entries.Add(key, value), cancellationToken).ConfigureAwait(false);

        public override async ValueTask<object> ImmutableSortedDictionaryFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            (await ReadAsync(stream, context, static _ => ImmutableSortedDictionary.CreateBuilder<TKey, TValue>(), static (entries, key, value) => entries.Add(key, value), cancellationToken).ConfigureAwait(false))
                .ToImmutable();

        // The room is no more than the context starts a collection with, as with an array; a dictionary grows as its entries arrive.
        private static async ValueTask<TEntries> ReadAsync<TEntries>(Stream stream, SerializationContext context, Func<int, TEntries> create,
            Action<TEntries, TKey, TValue> add, CancellationToken cancellationToken)
        {
            var len = await ReadCountAsync(stream, context, cancellationToken).ConfigureAwait(false);
            var entries = create(Math.Clamp(len, 0, context.InitialCollectionCapacity));
            if (len <= 0)
                return entries;

            var key = Serializer.Resolve(typeof(TKey), context);
            var item = Serializer.Resolve(typeof(TValue), context);
            for (int i = 0; i < len; i++)
            {
                var read = await Serializer.ReadResolvedAsync<TKey>(stream, key, context, cancellationToken).ConfigureAwait(false);
                if (read is null)
                    throw new SerializationException("Dictionary key cannot be null");

                add(entries, read, (await Serializer.ReadResolvedAsync<TValue>(stream, item, context, cancellationToken).ConfigureAwait(false))!);
            }

            return entries;
        }
    }
}
