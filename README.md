# Serialize

## General information

This is a binary serialization library. Originally it was designed to simplify communication with devices but later was extended to support more types and to be able to be used in an RPC library. The library supports all basic types and provides extensions for adding custom serializers as well as interfaces which allow making any type serializable.

## Get started

### Add reference to NuGet package

To start using the library, add a reference to the NuGet package to your project.

```xml
<PackageReference Include="Sigurn.Serialize" Version="1.0.1" />
```

### Serialize and deserialize data

In order to save any type to a stream, use the serializer.

```csharp
using System.IO;
using System.Threading.Tasks;
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

        Console.WriteLine(str);
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

Serialization context allows you to define different serialization parameters:

1. **ByteOrder** — defines byte order for simple type serializations: `ByteOrder.LittleEndian` or `ByteOrder.BigEndian`.
2. **UuidForm** — way of serializing `Guid` type. It can be `UuidForm.Microsoft` or `UuidForm.Linux`. Windows and Linux have different memory representations of `Guid`.
3. **AllowNullValues** — flag that defines if `null` values are allowed in serialization. If `null` values are allowed, then the serializer adds a boolean flag before every reference type or nullable type value to define if there is a value or `null` value is stored. If `null` values are not allowed, then serializing any `null` value will throw an exception.
4. **Encoding** — this property defines encoding which should be used for string serialization.
5. **TypeSerializers** — array of type serializers which should be used for type serialization. This list is used first before using global serializers and allows redefining default type serializers and adding custom type serializers as well.

Default serialization context defines the following values:

```csharp
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

There are several ways to make a custom type serializable.

### Implement interface ISerializable

If you have control over the type, then you can implement the interface `ISerializable` for your type. The type should also have a constructor without arguments so that the Serializer can create an instance of the type.

```csharp
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

If you don't have control over the type or the type does not have a constructor without parameters, then you can implement a serializer for that type and register the serializer globally or in the serialization context to be able to serialize that type.

```csharp
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
            Prop1 = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken),
            Prop2 = await Serializer.FromStreamAsync<bool>(stream, context, cancellationToken),
            Prop3 = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken) ?? string.Empty
        };
    }
}
```

Then you need to register your custom serializer.

To register it globally, use the `RegisterSerializer` method:

```csharp
Serializer.RegisterSerializer<MyCustomType>(() => new MyCustomTypeSerializer());
```

If you don't want to register the serializer globally, you may register it to the serialization context:

```csharp
var context = SerializationContext.Default with
{
    TypeSerializers = [ new MyCustomTypeSerializer() ]
};
```

### Generate type serializers

You can use a generator to generate a serializer for your type.

Use the `GenerateSerializer` attribute applied to your type to generate a serializer.

```csharp
[GenerateSerializer]
public class MyCustomType
{
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```

By default, the generated serializer is also registered in the global context, so it will be used application-wide for serializations. If for some reason you don't want to register the serializer in the global context and want to use it in some local serialization contexts, then you can set `useGlobally` to `false` in the `GenerateSerializer` attribute.

```csharp
[GenerateSerializer(false)]
public class MyCustomType
{
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```

Later, when you need the serializer for your local serialization context, you can get the serializer instance using the `Serializer.GetSerializer<T>()` method.

```csharp
var mySerializer = Serializer.GetSerializer<MyCustomType>();
var context = SerializationContext.Default with
{
    TypeSerializers = [ mySerializer ]
};
```

#### Define properties order

You can also define the property order for the generated serializer using the `SerializeOrder` attribute applied to type properties.

```csharp
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

If you want to ignore some properties and exclude them from serialization — for example, if you have calculated properties that should not be serialized because they are derived from other properties — you can use the `SerializeIgnore` attribute applied to the property.

```csharp
[GenerateSerializer]
public class MyCustomType
{
    [SerializeIgnore]
    public int Prop1 { get; init; }

    public bool Prop2 { get; init; }

    public string Prop3 { get; init; }
}
```