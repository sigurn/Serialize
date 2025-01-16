using Sigurn.Serialize;

namespace Sigurn;

/// <summary>
/// The interface is aimed for classes that should be serialized.
/// <para>Implementation of this interface allowes a class to be serialized by calling
/// <see cref="Sigurn.Serialize.Serializer.ToStreamAsync"/> method</para>
/// </summary>
/// <remarks>
/// <para>If you like your class to be serialized to stream or restored from stream
/// you should implemet this interface.</para>
/// </remarks>
/// <seealso cref="Sigurn.Serialize.Serializer"/>
public interface ISerializable
{
    /// <summary>
    /// Serializes object to stream.
    /// </summary>
    /// <param name="stream">Stream where object will be serialized to.</param>
    /// <remarks>
    /// <para>This method should store the state of the class or structure to stream.</para>
    /// <para>You should bear in mind during the implememtation of this method
    /// that all information necessary for restoring the original state of the object should be stored in the stream
    /// because you don't have any other storages like context or so that could help you in this.</para>
    /// <para>Also it is importatant to remember that you should not change the position in the stream.</para>
    /// <para>When this method is called the position in the stream is already set at the point
    /// where you should store the object's data.</para>
    /// <note type="implementnotes">To implement this method it's stronly recommended to use <see cref="Serialize.ValueToStream{T}"/> method.</note>
    /// </remarks>
    Task ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Deserializes object from stream.
    /// </summary>
    /// <param name="stream">Stream that containes serialized data.</param>
    /// <remarks>
    /// <para>This method should restore the state of the class or structure from stream.</para>
    /// <para>When this method is called the position in the stream is already set at the point
    /// where you should restore the object's data from. After this method call the position in the stream
    /// should be set at the byte following the last byte of the object's data.</para>
    /// <note type="implementnotes">To implement this method it's strongly recommended to use <see cref="Serializer.FromStreamAsync{T}(System.IO.Stream)"/> method.</note>
    /// </remarks>
    Task FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken);
};
