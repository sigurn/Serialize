using System.Collections.Immutable;
using Xunit;

namespace Sigurn.Serialize.Tests;

public class ImmutableCollectionsTests
{
    private static readonly SerializationContext _context = new() { AllowNullValues = true, ByteOrder = ByteOrder.BigEndian };

    private static async Task<byte[]> WrittenAsync<T>(T value)
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, _context, CancellationToken.None);
        return stream.ToArray();
    }

    private static async Task<T?> ReadAsync<T>(byte[] bytes) =>
        await Serializer.FromStreamAsync<T>(new MemoryStream(bytes), _context, CancellationToken.None);

    [Fact]
    public async Task AnImmutableList_IsWrittenAsAListOfItsElementsIs_AndReadsBack()
    {
        var values = ImmutableList.Create("a", null, "c");

        var bytes = await WrittenAsync(values);

        Assert.Equal(await WrittenAsync(new List<string?> { "a", null, "c" }), bytes);
        Assert.Equal(values, await ReadAsync<ImmutableList<string?>>(bytes));
    }

    [Fact]
    public async Task AnImmutableListAsItsInterface_ReadsBackAsAnImmutableList()
    {
        IImmutableList<int> values = ImmutableList.Create(3, 1, 2);

        var read = await ReadAsync<IImmutableList<int>>(await WrittenAsync(values));

        Assert.IsType<ImmutableList<int>>(read);
        Assert.Equal(values, read);
    }

    [Fact]
    public async Task AnAbsentImmutableList_ReadsBackAbsent()
    {
        Assert.Null(await ReadAsync<ImmutableList<int>>(await WrittenAsync<ImmutableList<int>?>(null)));
    }

    [Fact]
    public async Task AnImmutableSortedSet_IsWrittenInItsOrder_AndReadsBack()
    {
        var values = ImmutableSortedSet.Create(5, -1, 3);

        var bytes = await WrittenAsync(values);

        Assert.Equal(await WrittenAsync(new List<int> { -1, 3, 5 }), bytes);
        Assert.Equal(values, await ReadAsync<ImmutableSortedSet<int>>(bytes));
    }

    [Fact]
    public async Task AnImmutableSortedDictionary_IsWrittenAsASortedDictionaryIs_AndReadsBack()
    {
        var values = ImmutableSortedDictionary.CreateRange(new Dictionary<string, int?> { ["b"] = 2, ["a"] = null, ["c"] = 3 });

        var bytes = await WrittenAsync(values);

        Assert.Equal(await WrittenAsync(new SortedDictionary<string, int?>(values)), bytes);
        Assert.Equal(values, await ReadAsync<ImmutableSortedDictionary<string, int?>>(bytes));
    }

    [Fact]
    public async Task AnEmptyImmutableSortedDictionary_ReadsBackEmpty()
    {
        Assert.Empty((await ReadAsync<ImmutableSortedDictionary<int, int>>(await WrittenAsync(ImmutableSortedDictionary<int, int>.Empty)))!);
    }

    [Fact]
    public async Task EveryKindOfDictionary_IsWrittenTheSameWay()
    {
        var pairs = new Dictionary<int, string> { [2] = "two", [1] = "one" };
        var sorted = new SortedDictionary<int, string>(pairs);
        var expected = await WrittenAsync(sorted);

        Assert.Equal(expected, await WrittenAsync(new SortedList<int, string>(pairs)));
        Assert.Equal(expected, await WrittenAsync<IReadOnlyDictionary<int, string>>(sorted));
        Assert.Equal(expected, await WrittenAsync(ImmutableSortedDictionary.CreateRange(pairs)));
    }

    [Fact]
    public async Task ADictionaryWithAKeyTwice_IsRefused()
    {
        var once = await WrittenAsync(new SortedDictionary<int, int> { [1] = 1, [2] = 2 });
        var twice = once.ToArray();
        // The second key, 2, made 1: past the dictionary's presence byte, its count, and the first (key, value) pair of four bytes each.
        twice[1 + 4 + 8 + 3] = 1;

        await Assert.ThrowsAnyAsync<ArgumentException>(() => ReadAsync<Dictionary<int, int>>(twice));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => ReadAsync<ImmutableSortedDictionary<int, int>>(twice));
    }
}
