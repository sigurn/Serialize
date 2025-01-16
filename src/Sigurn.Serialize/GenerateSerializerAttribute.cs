namespace Sigurn.Serialize;

/// <summary>
/// Attributes which can be used to mark types in order to automatically generate serializer for the type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateSerializerAttribute : Attribute
{
}