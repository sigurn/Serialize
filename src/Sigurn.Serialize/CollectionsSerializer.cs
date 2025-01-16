
using System.Collections;

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

    public async Task<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(cancellationToken);

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

    public async Task ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(cancellationToken);
        
        if (type.IsArray)
        {
            var itemType = type.GetElementType() ?? throw new SerializationException("Type of array element cannot be null");
            Array array = (Array)value;
            await Serializer.ToStreamAsync(stream, array.Length, context, cancellationToken);
            foreach(var item in array)
                await Serializer.ToStreamAsync(stream, itemType, item, context, cancellationToken);
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

    private async Task<object> ArrayFromStream(Stream stream, Type elementType, SerializationContext context, CancellationToken cancellationToken)
    {
        var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        var array = Array.CreateInstance(elementType, len);
        for(int i=0; i<len; i++)
            array.SetValue(await Serializer.FromStreamAsync(stream, elementType, context, cancellationToken), i);
        return array;
    }

    private async Task<object> ListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        var genericType = typeof(List<>);
        var elementType = type.GetGenericArguments()[0];
        var listType = genericType.MakeGenericType(elementType);
        var ctor = listType.GetConstructor([]);
        if (ctor is null)
            throw new SerializationException($"Cannot create an instance of type {type}");

        IList list = (IList)ctor.Invoke([]);
        var len = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        for(int i=0; i<len; i++)
            list.Add(await Serializer.FromStreamAsync(stream, elementType, context, cancellationToken));

        return list;
    }

    private async Task<object> HashSetFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> StackFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> QueueFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> SortedSetFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> LinkedListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> DictionaryFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> SortedDictionaryFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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

    private async Task<object> SortedListFromStream(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
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
