namespace Sigurn.Serialize;

[AttributeUsage(AttributeTargets.Property)]
public class SerializeOrderAttribute : Attribute
{
    public int OrderId { get; }

    public SerializeOrderAttribute(int orderId)
    {
        OrderId = orderId;
    }
}