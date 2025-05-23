namespace Sigurn.Serialize;

/// <summary>
/// Attributes which can be used to mark types in order to automatically generate serializer for the type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateSerializerAttribute : Attribute
{
    /// <summary>
    /// Defines if the serializer should be used grobally for serialization.
    /// </summary>
    public bool UseGlobally { get; }

    /// <summary>
    /// Initializes a new instance of the class
    /// </summary>
    /// <param name="useGlobally">Defines if generated serializer should be used globally for serialization.</param>
    public GenerateSerializerAttribute(bool useGlobally = true)
    {
        UseGlobally = useGlobally;
    }
}