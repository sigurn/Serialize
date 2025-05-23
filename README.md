# Serialize

## General information
This is a binary serialization library. Originarry it was designed to simplify communication with devices but later was extended to support more types and to be able to be used in RPC library. The library supports all basic types and provides extensions for adding custom serializers as well as interfaces which allow make any type serializable.

## Get started

### Add reference to NuGet package
To start using the library add reference to NuGet package to your ptoject.

```XML
<PackageReference Include="Sigurn.Serialize" Version="1.0.1" />
```

### Serialize and deserialize data
In order to save any type to stream use serializer.

```C#
using System.IO;
using System.Threadint.Tasks;
using Sigurn.Serialize;

public static class Program
{
    public static async Task MainAsync(string[] args)
    {
        using var stream = new MemoryStream();

        // Serialize string to stream
        await Serializer.ToStreamAsync<string>(stream, "Hello world!", SerializationContext.Default, CancellationToken.None);

        stream.Seek(0, SeekOrigin.Begin);

        // Deserialize string from stream
        var str = await Serializer.FromStreamAsync<string>(stream, SerializationContext.Default, CancellationToken.None);

        Console.WrileLine(str);
    }
}
```

## Supported types
| Type            | Size (bytes)                               |
|-----------------|--------------------------------------------|
| bool            | 1                                          |
| sbyte           | 1                                          |
| byte            | 1                                          |
| short           | 2                                          |
| ushort          | 2                                          |
| int             | 4                                          |
| uint            | 4                                          |
| long            | 8                                          |
| ulong           | 8                                          |
| float           | 4                                          |
| double          | 8                                          |
| decimal         | 16                                         |
| string          | 4 + string size in defined encoding        |
| DateTime        | 8                                          |
| TimeSpan        | 8                                          |
| DateOnly        | 4                                          |
| TimeOnly        | 8                                          |
| Guid            | 16                                         |
| Version         | 16                                         |
| Array<T>        | 4 + array size * T size                    |
| List<T>         | 4 + list size * T size                     |
| Dictionary<K,V> | 4 + dictionary size * (K size + V size)    |
| Nullable<T>     | 1 + T size (if null values are allowed)    |
| Reference type  | 1 + type size (if null values are allowed) |


## Serialization context
Serialization context allows to define different serialization parameters

 1. **ByteOrder** -- defines byte order for simple type serializations `ByteOrder.LittleEndian` or `ByteOrder.BigEndian`.
 1. **UuidForm** -- way of serializing `Guid` type. It can be `UuidForm.Microsoft` or `UuidForm.Linux`. Windows and linux have different memory representation of `Guid`.
 1. **AllowNullValues** -- flag defines if `null` values are allowed in serialization. If `null` values are allowed then serializer adds boolean flag before every reference type or nullable type value to define if there is a value or `null` value is stored. I `null` values are not allowed then serializing any `null` value will throw exception.
 1. **Encoding** -- this property defines encoding which should be used for strings serialization.
 1. **TypeSerializers** -- array of type serializers which should be used for types serializations. This list is used first before using global serializers and allows to redefine default type serializers and add custom types serializers as well.

 Default serialization context defines the following values:
```C#
public static readonly SerializationContext Default = new SerializationContext()
{
    ByteOrder = ByteOrder.BigEndian,
    UuidForm = UuidForm.Linux,
    Encoding = Encoding.UTF8,
    AllowNullValues = true,
    TypeSerializers = []
};
```

## How to make custom type serializable
There are several ways how to make a custom type serializable.

### Implement interface ISerializable
If you have control over the type then you can implement interface ISerializable for your type.
The type also should have contructor without arguments that the Serializer could create an instance of the type.

```C#
public class MyCustomType : ISerializable
{
    public int Prop1 { get; set; }

    public bool Prop2 { get; set; }

    public string Prop3 { get; set; } = string.Empty;

    public async Task ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, Prop1, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, Prop2, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, Prop3, context, cancellationToken);
    }

    public async Task FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        Prop1 = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        Prop2 = await Serializer.FromStreamAsync<bool>(stream, context, cancellationToken);
        Prop3 = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken) ?? string.Empty;
    }
}
```

### Create type serializer
If you don't have control over the type or the type does not have constructor without parameters the you can implement a serializer for that type and register serializer globally or in the serializtion context to be able to serialize that type.

```C#
public class MyCustomType
{
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}

public class MyCustomTypeSerializer : ITypeSerializer<MyCustomType>
{
    public async Task ToStreamAsync(Stream stream, MyCustomType value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, value.Prop1, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Prop2, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Prop3, context, cancellationToken);
    }
    
    public async Task<MyCustomType> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        return new MyCustomType()
        {
            Prop1 = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
            Prop2 = await Serializer.FromStreamAsync<bool>(stream, context, cancellationToken);
            Prop3 = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken) ?? string.Empty;
        }
    }
}
```

Then you need to register your custom serializer.
To register it globally use `RegisterSerializer` method.
```C#
Serializer.RegisterSerializer<MyCustomType>(() => new MyCustomTypeSerializer());
```

If you don't want to register serializer globally you may register it to the serialization context.
```C#
var context = SerializationContext.Default with
{ 
    TypeSerializers = [ new MyCustomTypeSerializer() ]
};
```

### Generate type serializers
You can use generator to generate serializer for your type.
Use `GenerateSerializer` attribute applied to your type to generate serializer.
```C#
[GenerateSerializer]
public class MyCustomType
{
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```
By default the generated serializer is also registered in the global context so it will be used pplication wide for serializations.
If for some reason you don't want to register serializer in the global context and want to use it in some local serialization contexts then you can set `useGlobally` to `false` in the `GenerateSerializer` attrbute.
```C#
[GenerateSerializer(false)]
public class MyCustomType
{
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```

Later when you need the serializer for your local serialization context you can get the serializer instance using `Serilizer.GetSerializer<T>()' method.
```C#
var mySerializer = Serializer.GetSerializer<MyCustomType>();
var context = SerializationContext.Default with
{
    TypeSerializers = [ mySerializer ]
}
```
#### Define properties order
You also can define properties order for generated serializer using attribute `SerializeOrder` applied to type propertis.
```C#
[GenerateSerializer]
public class MyCustomType
{
    [SerializeOrder(3)]
    public int Prop1 { get; init; }

    [SerializeOrder(2)]
    public bool Prop2 { get; init; }

    [SerializeOrder(1)]
    public string Prop3 { get; init; }
}
```

#### Ignore properties
If you want to ignore some properties and exclude them from serialization, for example, if you have calculated properties that should not be serialized because they are calculated from other properties, you can use `SerializeIgnore` attribute applied to the property.
```C#
[GenerateSerializer]
public class MyCustomType
{
    [SerializeIgnore]
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```