namespace Sigurn.Serialize;

/// <summary>
/// Interface which describes functionality of the general serializer which can be used for serializing several types.
/// </summary>
public interface IGeneralSerializer : ITypeSerializer
{
    /// <summary>
    /// Checks if the type is supported by the serializer class.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>true if the type is supported otherwise false.</returns>
    bool IsTypeSupported(Type type);

    /// <inheritdoc/>
    Type ITypeSerializer.Type => typeof(object);
}

