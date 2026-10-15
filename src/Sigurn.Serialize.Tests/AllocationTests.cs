using Xunit;

namespace Sigurn.Serialize.Tests;

public class AllocationTests
{
    private const int Count = 100_000;

    public static IEnumerable<object[]> ByteOrders => [[ByteOrder.LittleEndian], [ByteOrder.BigEndian]];

    [OptimizedOnlyTheory]
    [MemberData(nameof(ByteOrders))]
    public async Task WritingALargeArray_SetsNothingAsidePerElement(ByteOrder byteOrder)
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = byteOrder };
        var values = Enumerable.Range(0, Count).Select(x => x * 0.5).ToArray();
        await Serializer.ToStreamAsync(Stream.Null, values, context, CancellationToken.None);

        var before = GC.GetAllocatedBytesForCurrentThread();
        await Serializer.ToStreamAsync(Stream.Null, values, context, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 16 * 1024, $"Writing {Count} doubles set aside {allocated} bytes.");
    }

    [OptimizedOnlyTheory]
    [MemberData(nameof(ByteOrders))]
    public async Task ReadingALargeArray_WithRoomForItFromTheStart_SetsAsideLittleMoreThanTheArray(ByteOrder byteOrder)
    {
        var context = new SerializationContext { AllowNullValues = false, ByteOrder = byteOrder, InitialCollectionCapacity = Count };
        var values = Enumerable.Range(0, Count).Select(x => x * 0.5).ToArray();
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, values, context, CancellationToken.None);
        stream.Position = 0;
        await Serializer.FromStreamAsync<double[]>(stream, context, CancellationToken.None);
        stream.Position = 0;

        var before = GC.GetAllocatedBytesForCurrentThread();
        var read = await Serializer.FromStreamAsync<double[]>(stream, context, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(values, read);
        Assert.True(allocated < Count * sizeof(double) + 16 * 1024, $"Reading {Count} doubles set aside {allocated} bytes.");
    }

    [OptimizedOnlyFact]
    public async Task ReadingAnArrayLargerThanItsInitialRoom_SetsAsideLessThanThreeTimesIt()
    {
        var context = new SerializationContext { AllowNullValues = false, InitialCollectionCapacity = 1024 };
        var values = Enumerable.Range(0, Count).Select(x => x * 0.5).ToArray();
        using var stream = new MemoryStream();
        await Serializer.ToStreamAsync(stream, values, context, CancellationToken.None);
        stream.Position = 0;
        await Serializer.FromStreamAsync<double[]>(stream, context, CancellationToken.None);
        stream.Position = 0;

        var before = GC.GetAllocatedBytesForCurrentThread();
        var read = await Serializer.FromStreamAsync<double[]>(stream, context, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // The room doubles as it fills: the rooms it outgrew add up to less than twice the array, and then there is the array itself.
        Assert.Equal(values, read);
        Assert.True(allocated < 3 * Count * sizeof(double) + 16 * 1024, $"Reading {Count} doubles set aside {allocated} bytes.");
    }
}
