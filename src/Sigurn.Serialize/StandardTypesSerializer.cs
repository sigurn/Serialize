namespace Sigurn.Serialize;

class StandardTypeSerializer<T> : ITypeSerializer<T>
{
    private static readonly ByteOrder _converterByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

    private readonly int _size;
    private readonly Func<T, byte[]> _toBytes;
    private readonly Func<byte[], T> _fromBytes;

    public StandardTypeSerializer(int size, Func<T, byte[]> toBytes, Func<byte[], T> fromBytes)
    {
        _size = size;
        _toBytes = toBytes;
        _fromBytes = fromBytes;
    }

    public async Task<T> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        byte[] data = new byte[_size];

        int len = await stream.ReadAsync(data, 0, data.Length, cancellationToken);
        if (len != _size)
            throw new SerializationException("Failed to read neccessary data from stream");

        if (_converterByteOrder != context.ByteOrder && data.Length > 1)
            data = data.Reverse().ToArray();

        return _fromBytes(data);
    }

    public async Task ToStreamAsync(Stream stream, T value, SerializationContext context, CancellationToken cancellationToken)
    {
        var data = _toBytes(value);

        if (_converterByteOrder != context.ByteOrder && data.Length > 1)
            data = data.Reverse().ToArray();

        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
    }
}

class StringSerializer : ITypeSerializer<string>
{
    public async Task<string> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = new byte[await Serializer.FromStreamAsync<int>(stream, context, cancellationToken)];
        int len = await stream.ReadAsync(buf, 0, buf.Length, cancellationToken);
        if (len != buf.Length)
            throw new SerializationException("Failed to read neccessary data from stream");

        return context.Encoding.GetString(buf);
    }

    public async Task ToStreamAsync(Stream stream, string value, SerializationContext context, CancellationToken cancellationToken)
    {
        var data = context.Encoding.GetBytes(value);
        await Serializer.ToStreamAsync(stream, data.Length, context, cancellationToken);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
    }
}

class GuidSerializer : ITypeSerializer<Guid>
{
    public async Task<Guid> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = new byte[16];
        int len = await stream.ReadAsync(buf, 0, buf.Length, cancellationToken);
        if (len != buf.Length)
            throw new SerializationException("Failed to read neccessary data from stream");

        return new Guid(buf, context.ByteOrder == ByteOrder.BigEndian || context.UuidForm == UuidForm.Linux);
    }

    public async Task ToStreamAsync(Stream stream, Guid value, SerializationContext context, CancellationToken cancellationToken)
    {
        var buf = new byte[16];
        if (!value.TryWriteBytes(buf, context.ByteOrder == ByteOrder.BigEndian || context.UuidForm == UuidForm.Linux, out int len))
            throw new SerializationException("Failed to serialize Guid");
        await stream.WriteAsync(buf, cancellationToken);
    }
}

class VersionSerializer : ITypeSerializer<Version>
{
    public async Task<Version> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
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

    public async Task ToStreamAsync(Stream stream, Version value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, value.Major, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Minor, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Build, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, value.Revision, context, cancellationToken);
    }
}

class DateSerializer : ITypeSerializer<DateOnly>
{
    public async Task<DateOnly> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        var year = await Serializer.FromStreamAsync<short>(stream, context, cancellationToken);
        var month = await Serializer.FromStreamAsync<byte>(stream, context, cancellationToken);
        var day = await Serializer.FromStreamAsync<byte>(stream, context, cancellationToken);

        return new DateOnly(year, month, day);
    }

    public async Task ToStreamAsync(Stream stream, DateOnly value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, (short)value.Year, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, (byte)value.Month, context, cancellationToken);
        await Serializer.ToStreamAsync(stream, (byte)value.Day, context, cancellationToken);
    }
}

class TimeSerializer : ITypeSerializer<TimeOnly>
{
    public async Task<TimeOnly> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)
    {
        long ticks = await Serializer.FromStreamAsync<long>(stream, context, cancellationToken);
        return new TimeOnly(ticks);
    }

    public async Task ToStreamAsync(Stream stream, TimeOnly value, SerializationContext context, CancellationToken cancellationToken)
    {
        await Serializer.ToStreamAsync(stream, value.Ticks, context, cancellationToken);
    }
}