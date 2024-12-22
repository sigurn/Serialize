using System.Collections;
using System.Collections.Generic;

namespace Sigurn.Serialize.Generator
{
    class TypePropertyInfo
    {
        public int OrderId { get; set; } = 0;
        public string Type { get; set; }
        public string Name { get; set; }
    }

    class TargetTypeInfo
    {
        public string TypeNamespace { get; set; }

        public string TypeName { get; set; }

        public string SerializerNamespace { get; set; }

        public string SerializerName { get; set; }

        public IList<TypePropertyInfo> Properties { get; } = new List<TypePropertyInfo>();
    }
}