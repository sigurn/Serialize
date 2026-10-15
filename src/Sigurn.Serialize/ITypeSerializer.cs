namespace Sigurn.Serialize;

/// <summary>
/// This interface describes functionality of type serializer.
/// </summary>
public interface ITypeSerializer
{
    /// <summary>
    /// Type which this serializer can serialize.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Stores and instance of the type to the stream.
    /// </summary>
    /// <param name="stream">Stream where the instance of the type will be stored.</param>
    /// <param name="type">Type of the instance in the <see cref="value"/> argument.</param>
    /// <param name="value">Instance of the type to store in the stream.</param>
    /// <param name="context">Serialization context which has serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Task to wait on for operation completion.</returns>
    ValueTask ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Loads and instance of the type from the stream.
    /// </summary>
    /// <param name="stream">Stream where the instance of the type will be loaded from.</param>
    /// <param name="type">Type of the instance which should be loaded from the stream.</param>
    /// <param name="context">Serialization context which has serialization settings.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Task to wait on for the operation completetion when where the operation result can be get from.</returns>
    ValueTask<object> FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken);
}

/// <summary>
/// This interface describes functionality of type serializer.
/// </summary>
/// <typeparam name="T">Type which this serializer can serialize.</typeparam>
public interface ITypeSerializer<T> : ITypeSerializer
{
    /// <summary>
    /// Stores and instance of the type to the stream.
    /// </summary>
    /// <param name="stream">Stream where the instance of the type will be stored.</param>
    /// <param name="value">Instance of the type to store in the stream.</param>
    /// <param name="context">Serialization context which has serializer settings.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Task to wait on for the operation completion.</returns>
    ValueTask ToStreamAsync(Stream stream, T value, SerializationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Loads an instance of the type from the stream.
    /// </summary>
    /// <param name="stream">Stream where the instance of the type will be loaded from.</param>
    /// <param name="context">Serialization context which has serializer settings.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Task to wait on for the operation completion, where the loaded instance can be got from.</returns>
    ValueTask<T> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

    Type ITypeSerializer.Type => typeof(T);

    ValueTask ITypeSerializer.ToStreamAsync(Stream stream, Type type, object value, SerializationContext context, CancellationToken cancellationToken)
    {
        if (type != typeof(T))
            throw new ArgumentException($"Unsupported type '{type}'. Expected type is '{typeof(T)}'");

        return ToStreamAsync(stream, (T)value, context, cancellationToken);
    }

    async ValueTask<object> ITypeSerializer.FromStreamAsync(Stream stream, Type type, SerializationContext context, CancellationToken cancellationToken)
    {
        if (type != typeof(T))
            throw new ArgumentException($"Unsupported type '{type}'. Expected type is '{typeof(T)}'");

        return await FromStreamAsync(stream, context, cancellationToken) 
            ?? throw new SerializationException("Serializer cannot return null");
    }
}