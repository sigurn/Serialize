using System.Buffers.Binary;
using System.Collections.Immutable;
using Xunit;

namespace Sigurn.Serialize.Tests;

public class LimitsTests
{
    private static readonly SerializationContext _context = new() { AllowNullValues = false, ByteOrder = ByteOrder.BigEndian };

    private static async Task<byte[]> WrittenAsync<T>(T value, SerializationContext context)
    {
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, value, context, CancellationToken.None);
        return stream.ToArray();
    }

    private static Task<T?> ReadAsync<T>(byte[] bytes, SerializationContext context) =>
        Serializer.FromStreamAsync<T>(new MemoryStream(bytes), context, CancellationToken.None).AsTask();

    // A count that claims far more than follows: the count itself, then the few bytes there are.
    private static byte[] Claiming(int count, int bytesThatFollow)
    {
        var bytes = new byte[sizeof(int) + bytesThatFollow];
        BinaryPrimitives.WriteInt32BigEndian(bytes, count);
        return bytes;
    }

    [Fact]
    public async Task AnArrayClaimingMoreElementsThanFollow_IsRefused_HavingSetAsideOnlyWhatItsBytesHold()
    {
        var bytes = Claiming(int.MaxValue, 10 * sizeof(double));

        var before = GC.GetAllocatedBytesForCurrentThread();
        await Assert.ThrowsAsync<SerializationException>(() => ReadAsync<double[]>(bytes, _context));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 2 * 1024 * 1024, $"Refusing the array set aside {allocated} bytes.");
    }

    [Fact]
    public async Task AStringClaimingMoreBytesThanFollow_IsRefused_HavingSetAsideOnlyWhatItsBytesHold()
    {
        var bytes = Claiming(int.MaxValue, 10);

        var before = GC.GetAllocatedBytesForCurrentThread();
        await Assert.ThrowsAsync<SerializationException>(() => ReadAsync<string>(bytes, _context));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 2 * 1024 * 1024, $"Refusing the string set aside {allocated} bytes.");
    }

    [Fact]
    public async Task AnArrayLargerThanTheInitialCapacity_ReadsBackWhole()
    {
        var context = _context with { InitialCollectionCapacity = 1000 };
        var values = Enumerable.Range(0, 2500).Select(x => x * 0.5).ToArray();

        Assert.Equal(values, await ReadAsync<double[]>(await WrittenAsync(values, context), context));
    }

    [Fact]
    public async Task AListAndASetLargerThanTheInitialCapacity_ReadBackWhole()
    {
        var context = _context with { InitialCollectionCapacity = 10 };
        var list = Enumerable.Range(0, 25).ToList();
        var set = Enumerable.Range(0, 25).ToHashSet();

        Assert.Equal(list, await ReadAsync<List<int>>(await WrittenAsync(list, context), context));
        Assert.Equal(set, await ReadAsync<HashSet<int>>(await WrittenAsync(set, context), context));
    }

    [Fact]
    public async Task AStringLargerThanTheInitialCapacity_ReadsBackWhole()
    {
        var context = _context with { InitialCollectionCapacity = 7 };
        var value = string.Concat(Enumerable.Repeat("Привет, ", 20));

        Assert.Equal(value, await ReadAsync<string>(await WrittenAsync(value, context), context));
    }

    [Fact]
    public async Task ACollectionLongerThanTheContextAllows_IsRefused_WrittenOrRead()
    {
        var limited = _context with { MaxCollectionLength = 3 };
        var bytes = await WrittenAsync(new[] { 1, 2, 3, 4 }, _context);

        await Assert.ThrowsAsync<SerializationException>(() => ReadAsync<int[]>(bytes, limited));
        await Assert.ThrowsAsync<SerializationException>(() => WrittenAsync(new[] { 1, 2, 3, 4 }, limited));
        await Assert.ThrowsAsync<SerializationException>(() => WrittenAsync(new List<int> { 1, 2, 3, 4 }, limited));
        await Assert.ThrowsAsync<SerializationException>(() => WrittenAsync(new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3, [4] = 4 }, limited));
        await Assert.ThrowsAsync<SerializationException>(
            () => ReadAsync<Dictionary<int, int>>(Claiming(4, 0), limited));
    }

    [Fact]
    public async Task ACollectionAsLongAsTheContextAllows_IsWrittenAndRead()
    {
        var limited = _context with { MaxCollectionLength = 3 };

        Assert.Equal(new[] { 1, 2, 3 }, await ReadAsync<int[]>(await WrittenAsync(new[] { 1, 2, 3 }, limited), limited));
    }

    [Fact]
    public async Task AStringLongerThanTheContextAllows_IsRefused_WrittenOrRead()
    {
        var limited = _context with { MaxStringBytes = 4 };
        var bytes = await WrittenAsync("12345", _context);

        await Assert.ThrowsAsync<SerializationException>(() => ReadAsync<string>(bytes, limited));
        await Assert.ThrowsAsync<SerializationException>(() => WrittenAsync("12345", limited));
        Assert.Equal("1234", await ReadAsync<string>(await WrittenAsync("1234", limited), limited));
    }

    [Fact]
    public void WithoutLimitsSet_NoneApplies_AndCollectionsStartAtTheirDefaultCapacity()
    {
        var context = new SerializationContext();

        Assert.Null(context.MaxCollectionLength);
        Assert.Null(context.MaxStringBytes);
        Assert.Equal(SerializationContext.DefaultInitialCollectionCapacity, context.InitialCollectionCapacity);
        Assert.Equal(SerializationContext.DefaultInitialCollectionCapacity, SerializationContext.Default.InitialCollectionCapacity);
    }

    [Fact]
    public void LimitsThatCannotHold_AreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerializationContext { InitialCollectionCapacity = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerializationContext { MaxCollectionLength = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerializationContext { MaxStringBytes = -1 });
    }

    [Fact]
    public async Task AnImmutableArray_IsWrittenAsTheArrayOfItsElementsIs_AndReadsBack()
    {
        ImmutableArray<int> values = [1, -2, 3];

        var bytes = await WrittenAsync(values, _context);

        Assert.Equal(await WrittenAsync(new[] { 1, -2, 3 }, _context), bytes);
        // An ImmutableArray equals another only by the array it holds, so the elements are what is compared.
        Assert.Equal(values.AsEnumerable(), (await ReadAsync<ImmutableArray<int>>(bytes, _context)).AsEnumerable());
    }

    [Fact]
    public async Task AnEmptyImmutableArray_ReadsBackEmpty_NotDefault()
    {
        var read = await ReadAsync<ImmutableArray<string>>(await WrittenAsync(ImmutableArray<string>.Empty, _context), _context);

        Assert.False(read.IsDefault);
        Assert.Empty(read);
    }

    [Fact]
    public async Task ADefaultImmutableArray_HoldsNoArray_AndIsRefused()
    {
        await Assert.ThrowsAsync<SerializationException>(() => WrittenAsync(default(ImmutableArray<int>), _context));
    }

    [Fact]
    public async Task AnImmutableArrayClaimingMoreElementsThanFollow_IsRefused()
    {
        await Assert.ThrowsAsync<SerializationException>(() => ReadAsync<ImmutableArray<double>>(Claiming(int.MaxValue, 8), _context));
    }
}
