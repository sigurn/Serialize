using System.Collections.Immutable;
using Sigurn;
using Sigurn.Serialize;
using Sigurn.Serialize.AotSmoke;

var order = new Order
{
    Id = Guid.NewGuid(),
    Numbers = [1, 2, 3],
    Prices = new Dictionary<string, decimal?> { ["tea"] = 1.5m, ["free"] = null },
    State = OrderState.Shipped,
    Quantity = null,
    Customer = new Customer { Name = "Ann" },
    Lines = ImmutableArray.Create(10L, 20L),
    Tags = new Stack<string>(["a", "b"]),
    Pair = new KeyValuePair<int, string>(7, "seven"),
    Matrix = [[1, 2], [3]],
};

var read = await RoundTripAsync(order);
Check(read is not null, "order");
Check(read!.Id == order.Id, "Id");
Check(read.Numbers.SequenceEqual(order.Numbers), "Numbers");
Check(read.Prices.OrderBy(p => p.Key).SequenceEqual(order.Prices.OrderBy(p => p.Key)), "Prices");
Check(read.State == order.State, "State");
Check(read.Quantity == order.Quantity, "Quantity");
Check(read.Customer?.Name == "Ann", "Customer");
Check(read.Lines.SequenceEqual(order.Lines), "Lines");
Check(read.Tags.SequenceEqual(order.Tags), "Tags");
Check(read.Pair.Equals(order.Pair), "Pair");
Check(read.Matrix.Length == 2 && read.Matrix[0].SequenceEqual([1, 2]) && read.Matrix[1].SequenceEqual([3]), "Matrix");

// Serialized only by hand, inside ISerializable, and registered nowhere: the generator registers Queue<Guid> from the call itself.
var shipment = new Shipment();
shipment.Parcels.Enqueue(Guid.NewGuid());
shipment.Parcels.Enqueue(Guid.NewGuid());
var shipped = await RoundTripAsync(shipment);
Check(shipped is not null && shipped.Parcels.SequenceEqual(shipment.Parcels), "Shipment");

var customer = await RoundTripAsync(new Customer { Name = "Bob" });
Check(customer?.Name == "Bob", "Customer on its own");

try
{
    await RoundTripAsync(new Queue<Customer>());
    Check(false, "an unregistered type is refused");
}
catch (SerializationException e) when (e.Message.Contains("Register"))
{
}

Console.WriteLine("OK");
return 0;

static async Task<T?> RoundTripAsync<T>(T value)
{
    using var stream = new MemoryStream();
    await Serializer.ToStreamAsync(stream, value, SerializationContext.Default, CancellationToken.None);
    stream.Position = 0;
    return await Serializer.FromStreamAsync<T>(stream, SerializationContext.Default, CancellationToken.None);
}

static void Check(bool ok, string what)
{
    if (ok)
        return;

    Console.Error.WriteLine($"FAILED: {what}");
    Environment.Exit(1);
}

namespace Sigurn.Serialize.AotSmoke
{
    public enum OrderState : byte { New = 1, Shipped = 2 }

    public sealed class Customer : ISerializable
    {
        public string Name { get; set; } = string.Empty;

        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Serializer.ToStreamAsync(stream, Name, context, cancellationToken);

        public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Name = await Serializer.FromStreamAsync<string>(stream, context, cancellationToken) ?? string.Empty;
    }

    public sealed class Shipment : ISerializable
    {
        public Queue<Guid> Parcels { get; set; } = new();

        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Serializer.ToStreamAsync(stream, Parcels, context, cancellationToken);

        public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Parcels = await Serializer.FromStreamAsync<Queue<Guid>>(stream, context, cancellationToken) ?? new();
    }

    [GenerateSerializer]
    public sealed class Order
    {
        public Guid Id { get; set; }
        public IReadOnlyList<int> Numbers { get; set; } = [];
        public Dictionary<string, decimal?> Prices { get; set; } = [];
        public OrderState State { get; set; }
        public int? Quantity { get; set; }
        public Customer? Customer { get; set; }
        public ImmutableArray<long> Lines { get; set; }
        public Stack<string> Tags { get; set; } = new();
        public KeyValuePair<int, string> Pair { get; set; }
        public int[][] Matrix { get; set; } = [];
    }
}
