namespace Sigurn.Serialize;

/// <summary>
/// Exception which is thrown when there a problem with the serialization.
/// </summary>
public class SerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="message">Message which describes the problem.</param>
    public SerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance on the class.
    /// </summary>
    /// <param name="message">Message which describs the problem.</param>
    /// <param name="innerException">Exception which caused this exception.</param>
    public SerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}