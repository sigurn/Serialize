using System.Buffers;

namespace Sigurn.Serialize;

delegate void ToBytes<T>(T value, Span<byte> bytes);

delegate T FromBytes<T>(ReadOnlySpan<byte> bytes);

class StandardTypeSerializer<T> : ITypeSerializer<T>
{
    private static readonly ByteOrder _converterByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

    private readonly int _size;
    private readonly ToBytes<T> _toBytes;
    private readonly FromBytes<T> _fromBytes;

    public StandardTypeSerializer(int size, ToBytes<T> toBytes, FromBytes<T> fromBytes)
    {
        _size = size;
        _toBytes = toBytes;
        _fromBytes = fromBytes;
    }

    public async ValueTask<T> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var data = ArrayPool<byte>.Shared.Rent(_size);
        try
        {
            await StreamBytes.ReadExactlyAsync(stream, data.AsMemory(0, _size), cancellationToken).ConfigureAwait(false);
            return Decode(data, context.ByteOrder);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    public async ValueTask ToStreamAsync(Stream stream, T value, SerializationContext context, CancellationToken cancellationToken)
    {
        var data = ArrayPool<byte>.Shared.Rent(_size);
        try
        {
            Encode(value, data, context.ByteOrder);
            await stream.WriteAsync(data.AsMemory(0, _size), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    private T Decode(byte[] data, ByteOrder byteOrder)
    {
        var bytes = data.AsSpan(0, _size);
        if (_converterByteOrder != byteOrder)
            bytes.Reverse();

        return _fromBytes(bytes);
    }

    private void Encode(T value, byte[] data, ByteOrder byteOrder)
    {
        var bytes = data.AsSpan(0, _size);
        _toBytes(value, bytes);
        if (_converterByteOrder != byteOrder)
            bytes.Reverse();
    }
}

class StringSerializer : ITypeSerializer<string>
{
    // The room is no more than the context starts a collection with, and grows as the bytes arrive, so a length that claims more than
    // follows runs out having set aside only what there was.
    public async ValueTask<string> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var length = Limited(await Serializer.FromStreamAsync<int>(stream, context, cancellationToken).ConfigureAwait(false), context);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var buf = new byte[Math.Min(length, context.InitialCollectionCapacity)];
        var read = 0;
        while (read < length)
        {
            if (read == buf.Length)
                Array.Resize(ref buf, (int)Math.Min(length, 2L * buf.Length));

            var piece = buf.Length - read;
            await StreamBytes.ReadExactlyAsync(stream, buf.AsMemory(read, piece), cancellationToken).ConfigureAwait(false);
            read += piece;
        }

        return context.Encoding.GetString(buf);
    }

    public async ValueTask ToStreamAsync(Stream stream, string value, SerializationContext context, CancellationToken cancellationToken)
    {
        var data = context.Encoding.GetBytes(value);
        await Serializer.ToStreamAsync(stream, Limited(data.Length, context), context, cancellationToken);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
    }

    private static int Limited(int bytes, SerializationContext context) =>
        context.MaxStringBytes is { } limit && bytes > limit
            ? throw new SerializationException($"A string of {bytes} bytes is longer than the {limit} the context allows.")
            : bytes;
}

class GuidSerializer : ITypeSerializer<Guid>
{
    public async ValueTask<Guid> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = new byte[16];
        await StreamBytes.ReadExactlyAsync(stream, buf, cancellationToken).ConfigureAwait(false);

        return new Guid(buf, context.ByteOrder == ByteOrder.BigEndian || context.UuidForm == UuidForm.Linux);
    }

    public async ValueTask ToStreamAsync(Stream stream, Guid value, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = new byte[16];
        if (!value.TryWriteBytes(buf, context.ByteOrder == ByteOrder.BigEndian || context.UuidForm == UuidForm.Linux, out int len))
            throw new SerializationException("Failed to serialize Guid");
        await stream.WriteAsync(buf, cancellationToken);
    }
}

class VersionSerializer : ITypeSerializer<Version>
{
    public async ValueTask<Version> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var major = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        var minor = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        var build = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);
        var revision = await Serializer.FromStreamAsync<int>(stream, context, cancellationToken);

        if (build < 0 && revision < 0)
            return new Version(major, minor);

        if (revision < 0)
            return new Version(major, minor, build);

        return new Version(major, minor, build, revision);
    }

    public async ValueTask ToStreamAsync(Stream stream, Version value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, value.Major, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Minor, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Build, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Revision, context, cancellationToken);
    }
}

class DateSerializer : ITypeSerializer<DateOnly>
{
    public async ValueTask<DateOnly> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var year = await Serializer.FromStreamAsync<short>(stream, context, cancellationToken);
        var month = await Serializer.FromStreamAsync<byte>(stream, context, cancellationToken);
        var day = await Serializer.FromStreamAsync<byte>(stream, context, cancellationToken);

        return new DateOnly(year, month, day);
    }

    public async ValueTask ToStreamAsync(Stream stream, DateOnly value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, (short)value.Year, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, (byte)value.Month, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, (byte)value.Day, context, cancellationToken);
    }
}

class TimeSerializer : ITypeSerializer<TimeOnly>
{
    public async ValueTask<TimeOnly> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        long ticks = await Serializer.FromStreamAsync<long>(stream, context, cancellationToken);
        return new TimeOnly(ticks);
    }

    public async ValueTask ToStreamAsync(Stream stream, TimeOnly value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, value.Ticks, context, cancellationToken);
    }
}