using System.Collections.Immutable;
using Xunit;

namespace Sigurn.Serialize.Tests;

// The serializers registered by the Register* helpers — those Native AOT relies on — write and read exactly what the reflective ones do.
public class TypedSerializersTests
{
    private static async Task<byte[]> WriteAsync(ITypeSerializer serializer, Type type, object value, SerializationContext context)
    {
        using var stream = new MemoryStream();
        await serializer.ToStreamAsync(stream, type, value, context, CancellationToken.None);
        return stream.ToArray();
    }

    private static async Task<object> ReadAsync(ITypeSerializer serializer, Type type, byte[] bytes, SerializationContext context)
    {
        using var stream = new MemoryStream(bytes);
        var value = await serializer.FromStreamAsync(stream, type, context, CancellationToken.None);
        Assert.Equal(stream.Length, stream.Position);
        return value;
    }

    public static IEnumerable<object[]> Collections()
    {
        yield return [typeof(int[]), new[] { 1, 2, 3 }, (Action)Serializer.RegisterArray<int>];
        yield return [typeof(string?[]), new[] { "a", null, "c" }, (Action)Serializer.RegisterArray<string?>];
        yield return [typeof(List<int>), new List<int> { 1, 2, 3 }, (Action)Serializer.RegisterList<int>];
        yield return [typeof(IList<int?>), new List<int?> { 1, null, 3 }, (Action)Serializer.RegisterList<int?>];
        yield return [typeof(IReadOnlyCollection<string>), new List<string> { "x", "y" }, (Action)Serializer.RegisterList<string>];
        yield return [typeof(HashSet<int>), new HashSet<int> { 5, 6 }, (Action)Serializer.RegisterHashSet<int>];
        yield return [typeof(ISet<int>), new HashSet<int> { 5, 6 }, (Action)Serializer.RegisterHashSet<int>];
        yield return [typeof(Stack<int>), new Stack<int>([1, 2, 3]), (Action)Serializer.RegisterStack<int>];
        yield return [typeof(Queue<int>), new Queue<int>([1, 2, 3]), (Action)Serializer.RegisterQueue<int>];
        yield return [typeof(SortedSet<int>), new SortedSet<int> { 3, 1, 2 }, (Action)Serializer.RegisterSortedSet<int>];
        yield return [typeof(LinkedList<int>), new LinkedList<int>([1, 2]), (Action)Serializer.RegisterLinkedList<int>];
        yield return [typeof(ImmutableArray<long>), ImmutableArray.Create(1L, 2L), (Action)Serializer.RegisterImmutableArray<long>];
        yield return [typeof(IImmutableList<int>), ImmutableList.Create(1, 2), (Action)Serializer.RegisterImmutableList<int>];
        yield return [typeof(ImmutableSortedSet<int>), ImmutableSortedSet.Create(2, 1), (Action)Serializer.RegisterImmutableSortedSet<int>];
        yield return [typeof(Dictionary<string, Guid>), new Dictionary<string, Guid> { ["a"] = Guid.NewGuid() }, (Action)Serializer.RegisterDictionary<string, Guid>];
        yield return [typeof(IReadOnlyDictionary<int, string?>), new Dictionary<int, string?> { [1] = null, [2] = "b" }, (Action)Serializer.RegisterDictionary<int, string?>];
        yield return [typeof(SortedDictionary<int, int>), new SortedDictionary<int, int> { [2] = 1, [1] = 2 }, (Action)Serializer.RegisterSortedDictionary<int, int>];
        yield return [typeof(SortedList<int, int>), new SortedList<int, int> { [2] = 1, [1] = 2 }, (Action)Serializer.RegisterSortedList<int, int>];
        yield return [typeof(ImmutableSortedDictionary<int, int>), ImmutableSortedDictionary.CreateRange([new KeyValuePair<int, int>(1, 2)]), (Action)Serializer.RegisterImmutableSortedDictionary<int, int>];
        yield return [typeof(KeyValuePair<int, string?>), new KeyValuePair<int, string?>(1, null), (Action)Serializer.RegisterKeyValuePair<int, string?>];
        yield return [typeof(KeyValuePair<byte?, byte?>), new KeyValuePair<byte?, byte?>(0, 0xff), (Action)Serializer.RegisterKeyValuePair<byte?, byte?>];
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public async Task ARegisteredGenericType_IsWrittenAndRead_AsByReflection(Type type, object value, Action register)
    {
        register();

        IGeneralSerializer reflective = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
            ? new KeyValuePairSerializer()
            : new CollectionsSerializer();

        foreach (var context in new[] { SerializationContext.Default, SerializationContext.Default with { AllowNullValues = false } })
        {
            if (!context.AllowNullValues && ((System.Collections.IEnumerable)(value is System.Collections.IEnumerable e ? e : new[] { value })).Cast<object?>().Any(HasNull))
                continue;

            var registered = context.FindTypeSerializer(type);
            Assert.NotNull(registered);
            Assert.IsNotType<CollectionsSerializer>(registered);
            Assert.IsNotType<KeyValuePairSerializer>(registered);

            var expected = await WriteAsync(reflective, type, value, context);
            var actual = await WriteAsync(registered, type, value, context);
            Assert.Equal(expected, actual);

            var read = await ReadAsync(registered, type, actual, context);
            Assert.IsAssignableFrom(type, read);
            Assert.Equal(expected, await WriteAsync(reflective, type, read, context));
        }
    }

    private static bool HasNull(object? item) =>
        item is null || (item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
            && item.GetType().GetProperty("Value")!.GetValue(item) is null);

    [Fact]
    public async Task ARegisteredSerializable_IsWrittenAndRead_AsByReflection()
    {
        Serializer.RegisterSerializable<Point>();
        var value = new Point { X = 3, Y = -4 };

        var expected = await WriteAsync(new SerializableTypeSerializer(typeof(Point)), typeof(Point), value, SerializationContext.Default);
        var registered = SerializationContext.Default.FindTypeSerializer(typeof(Point));
        Assert.IsType<SerializableTypeSerializer<Point>>(registered);

        var actual = await WriteAsync(registered!, typeof(Point), value, SerializationContext.Default);
        Assert.Equal(expected, actual);
        Assert.Equal(value, await ReadAsync(registered!, typeof(Point), actual, SerializationContext.Default));
    }

    [Fact]
    public void ASerializerRegisteredByTheUser_TakesPrecedence_OverOneRegisteredByAHelper()
    {
        Serializer.RegisterList<UserListItem>();
        var user = new UserListSerializer();
        Serializer.RegisterSerializer<List<UserListItem>>(user);

        Assert.Same(user, SerializationContext.Default.FindTypeSerializer(typeof(List<UserListItem>)));
        Assert.Same(user, Serializer.GetSerializer<List<UserListItem>>());
    }

    [Fact]
    public void ATypeRegisteredByAHelper_IsNotSeenByGetSerializer_AndRegisteringItTwiceDoesNothing()
    {
        Serializer.RegisterHashSet<UserListItem>();
        Serializer.RegisterHashSet<UserListItem>();

        Assert.NotNull(SerializationContext.Default.FindTypeSerializer(typeof(HashSet<UserListItem>)));
        Assert.Throws<ArgumentException>(() => Serializer.GetSerializer<HashSet<UserListItem>>());
    }

    // A class: an async method of a struct works on a copy of it, so a struct could not read itself through ISerializable.
    public sealed record class Point : ISerializable
    {
        public int X { get; set; }
        public int Y { get; set; }

        public async ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            await Serializer.ToStreamAsync(stream, X, context, cancellationToken);
            await Serializer.ToStreamAsync(stream, Y, context, cancellationToken);
        }

        public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            X = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
            Y = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        }
    }

    public sealed class UserListItem;

    private sealed class UserListSerializer : ITypeSerializer<List<UserListItem>>
    {
        public ValueTask ToStreamAsync(Stream stream, List<UserListItem> value, SerializationContext context, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<List<UserListItem>> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new List<UserListItem>());
    }
}

[CollectionDefinition(nameof(WithoutReflection), DisableParallelization = true)]
public sealed class WithoutReflection;

// Where code cannot be made at run time (Native AOT) only registered types are serialized; the rest are refused with a hint.
[Collection(nameof(WithoutReflection))]
public sealed class WithoutReflectionTests : IDisposable
{
    public WithoutReflectionTests() => Serializer.ReflectionFallbackDisabled = true;

    public void Dispose() => Serializer.ReflectionFallbackDisabled = false;

    public enum Colour : short { Red = 1, Green = 2 }

    public sealed class NeverRegistered;

    private static async Task<T?> RoundTripAsync<T>(T value)
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, SerializationContext.Default, CancellationToken.None);
        stream.Position = 0;
        return await Serializer.FromStreamAsync<T>(stream, SerializationContext.Default, CancellationToken.None);
    }

    [Fact]
    public async Task AnUnregisteredCollection_IsRefused_WithAHint()
    {
        var error = await Assert.ThrowsAsync<SerializationException>(() => RoundTripAsync(new Queue<NeverRegistered>()));
        Assert.Contains("Register", error.Message);
    }

    [Fact]
    public async Task ARegisteredCollection_IsWrittenAndRead()
    {
        Serializer.RegisterDictionary<string, List<Guid?>>();
        Serializer.RegisterList<Guid?>();
        var value = new Dictionary<string, List<Guid?>> { ["a"] = [Guid.NewGuid(), null] };

        Assert.Equal(value, await RoundTripAsync(value));
    }

    // A type implementing ISerializable without a parameterless constructor (SGS001) is served by a serializer registered for it.
    [Fact]
    public async Task ASerializableTheGeneratorCannotMake_IsServedByARegisteredSerializer()
    {
        Serializer.RegisterSerializer<Named>(new NamedSerializer());

        Assert.Equal("x", (await RoundTripAsync(new Named("x")))?.Name);
    }

    public sealed class Named(string name) : ISerializable
    {
        public string Name { get; private set; } = name;

        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Serializer.ToStreamAsync(stream, Name, context, cancellationToken);

        public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Name = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken) ?? string.Empty;
    }

    private sealed class NamedSerializer : ITypeSerializer<Named>
    {
        public ValueTask ToStreamAsync(Stream stream, Named value, SerializationContext context, CancellationToken cancellationToken) =>
            value.ToStreamAsync(stream, context, cancellationToken);

        public async ValueTask<Named> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
        {
            var value = new Named(string.Empty);
            await value.FromStreamAsync(stream, context, cancellationToken);
            return value;
        }
    }

    [Fact]
    public async Task AnEnum_NeedsNoRegistration()
    {
        Assert.Equal(Colour.Green, await RoundTripAsync(Colour.Green));
        Assert.Equal((Colour?)null, await RoundTripAsync((Colour?)null));
    }
}
