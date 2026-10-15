
using System.Collections;
using System.Collections.Concurrent;

namespace Sigurn.Serialize;

class CollectionsSerializer : IGeneralSerializer
{
    private static readonly Type[] _genericTypes = [typeof(List<>), typeof(HashSet<>), 
        typeof(Stack<>), typeof(Queue<>), typeof(SortedSet<>), typeof(LinkedList<>), 
        typeof(IReadOnlyList<>), typeof(IList<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>),
        typeof(ISet<>), typeof(IReadOnlySet<>),
        typeof(Dictionary<,>), typeof(SortedDictionary<,>), typeof(SortedList<,>), typeof(IReadOnlyDictionary<,>)];

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
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return await DictionaryFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedDictionary<,>))
            return await SortedDictionaryFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedList<,>))
            return await SortedListFromStream(stream, type, context, cancellationToken);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            return await DictionaryFromStream(stream, type, context, cancellationToken);

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
            await Serializer.ToStreamAsync(stream, array.Length, context, cancellationToken);
            foreach(var item in array)
                await Serializer.ToStreamAsync(stream, itemType, item, context, cancellationToken);
        }
        else if (type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
            await Items.Of(type.GetGenericArguments()[0]).ToStreamAsync(stream, value, context, cancellationToken);
        }
        else if (type.IsAssignableTo(typeof(IList)))
        {
            Type? itemType = null;
            if (type.IsGenericType)
                itemType = type.GetGenericArguments()[0];
            IList list = (IList)value;
            await Serializer.ToStreamAsync(stream, list.Count, context, cancellationToken);
            foreach(var item in list)
                await Serializer.ToStreamAsync(stream, itemType ?? item.GetType(), item, context, cancellationToken);
        }
        else if (type.IsAssignableTo(typeof(IDictionary)))
        {
            Type? keyType = null;
            Type? valueType = null;
            if (type.IsGenericType)
            {
                keyType = type.GetGenericArguments()[0];
                valueType = type.GetGenericArguments()[1];
                IDictionary dic = (IDictionary)value;
                await Serializer.ToStreamAsync(stream, dic.Count, context, cancellationToken);
                var e = dic.GetEnumerator();
                while(e.MoveNext())
                {
                    await Serializer.ToStreamAsync(stream, keyType, e.Key, context, cancellationToken);
                    await Serializer.ToStreamAsync(stream, valueType, e.Value, context, cancellationToken);
                }
            }
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
            await Serializer.ToStreamAsync(stream, count, context, cancellationToken);
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
            await Serializer.ToStreamAsync(stream, count, context, cancellationToken);
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
    }

    // The serializer of the elements is found once for the whole collection, and only when it has an element to find it for: an
    // empty collection of a type no serializer knows is still written and read, as it always was.
    private sealed class Items<TItem> : Items
    {
        public override async ValueTask ToStreamAsync(Stream stream, object value, SerializationContext context, CancellationToken cancellationToken)
        {
            switch (value)
            {
                case IReadOnlyList<TItem> list:
                    await Serializer.ToStreamAsync(stream, list.Count, context, cancellationToken).ConfigureAwait(false);
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
                    await Serializer.ToStreamAsync(stream, count, context, cancellationToken).ConfigureAwait(false);
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

        public override async ValueTask<object> ArrayFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken).ConfigureAwait(false);
            ArgumentOutOfRangeException.ThrowIfNegative(len);
            var array = new TItem[len];
            if (len == 0)
                return array;

            var resolved = Serializer.Resolve(typeof(TItem), context);
            for (int i = 0; i < len; i++)
                array[i] = (await Serializer.ReadResolvedAsync<TItem>(stream, resolved, context, cancellationToken).ConfigureAwait(false))!;
            return array;
        }

        public override async ValueTask<object> ListFromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken).ConfigureAwait(false);
            var list = new List<TItem>();
            if (len <= 0)
                return list;

            var resolved = Serializer.Resolve(typeof(TItem), context);
            for (int i = 0; i < len; i++)
                list.Add((await Serializer.ReadResolvedAsync<TItem>(stream, resolved, context, cancellationToken).ConfigureAwait(false))!);
            return list;
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

    private async ValueTask<object> DictionaryFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(Dictionary<,>);
        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];
        var dicType = genericType.MakeGenericType(keyType, valueType);
        var ctor = dicType.GetConstructor([]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        IDictionary dic = (IDictionary)ctor.Invoke([]);
        var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        for(int i=0; i<len; i++)
        {
            var key = await Serializer.FromStreamAsync(stream, keyType, context, cancellationToken);
            if (key is null)
                throw new SerializationException("Dictionary key cannot be null");
            var value = await Serializer.FromStreamAsync(stream, valueType, context, cancellationToken);
            dic.Add(key, value);
        }

        return dic;
    }

    private async ValueTask<object> SortedDictionaryFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(SortedDictionary<,>);
        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];
        var dicType = genericType.MakeGenericType(keyType, valueType);
        var ctor = dicType.GetConstructor([]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        IDictionary dic = (IDictionary)ctor.Invoke([]);
        var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        for(int i=0; i<len; i++)
        {
            var key = await Serializer.FromStreamAsync(stream, keyType, context, cancellationToken);
            if (key is null)
                throw new SerializationException("Dictionary key cannot be null");
            var value = await Serializer.FromStreamAsync(stream, valueType, context, cancellationToken);
            dic.Add(key, value);
        }

        return dic;
    }

    private async ValueTask<object> SortedListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(SortedList<,>);
        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];
        var dicType = genericType.MakeGenericType(keyType, valueType);
        var ctor = dicType.GetConstructor([]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        IDictionary dic = (IDictionary)ctor.Invoke([]);
        var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        for(int i=0; i<len; i++)
        {
            var key = await Serializer.FromStreamAsync(stream, keyType, context, cancellationToken);
            if (key is null)
                throw new SerializationException("Dictionary key cannot be null");
            var value = await Serializer.FromStreamAsync(stream, valueType, context, cancellationToken);
            dic.Add(key, value);
        }

        return dic;
    }
}
