using System;

namespace Sigurn.Serialize;

/// <summary>
/// The attribute which allows to exclude some class properties from the generated automatic serializer.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SerializeIgnoreAttribute : Attribute
{
}
