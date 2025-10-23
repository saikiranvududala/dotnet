public record OrderItem(int ProductId, int Quantity, decimal UnitPrice);
public record CreateOrderRequest(int ProductId, int Quantity);
public record Order(int Id, int ProductId, int Quantity, decimal Total);

public record OrderCreatedEvent(int OrderId, int ProductId, int Quantity, decimal Total);
