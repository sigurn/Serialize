namespace Sigurn.Serialize;

/// <summary>
/// Defines possible byte orders for serialization.
/// </summary>
public enum ByteOrder : byte
{
    /// <summary>
    /// Little-endian byte order.
    /// </summary>
    LittleEndian = 0x01,

    /// <summary>
    /// Big-endian byte order.
    /// </summary>
    BigEndian = 0x10,

    /// <summary>
    /// Byte order used bye Intel achitecture (i.e. little-endian byte order).
    /// </summary>
    Intel = LittleEndian,

    /// <summary>
    /// Byte order used by TCP/IP protocol (i.e. big-endian byte order).
    /// </summary>
    Network = BigEndian,

    /// <summary>
    /// Byte order used by Motorola architecture (i.e. big-endian byte order).
    /// </summary>
    Motorola = BigEndian,

    /// <summary>
    /// Byte order used by Apple silicon (i.e. little-endian byte order).
    /// </summary>
    Apple = LittleEndian,
}
