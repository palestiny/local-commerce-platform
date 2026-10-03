using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus=LocalCommerce.Domain.Delivery.DeliveryStatus;
using OrderEntity=LocalCommerce.Domain.Ordering.Order;
using OrderStatus=LocalCommerce.Domain.Ordering.OrderStatus;

namespace LocalCommerce.Application.Ordering;

public sealed record CancelOrderCommand(Guid OrderId,Guid ActorId,string IdempotencyKey);
public sealed record CancelOrderResult(Guid OrderId,Guid? DeliveryId);
public sealed class CancelOrderRejectedException:Exception{public CancelOrderRejectedException(string message):base(message){}}

public interface IOrderCancellationOrderRepository{Task<OrderEntity?> GetAsync(Guid orderId,CancellationToken ct);Task SaveAsync(OrderEntity order,CancellationToken ct);}
public interface ICancelOrderDeliveryRepository{Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId,CancellationToken ct);Task SaveAsync(DeliveryEntity delivery,CancellationToken ct);}
public interface ICancelOrderAuthorization{Task<bool> CanCancelAsync(Guid actorId,OrderEntity order,CancellationToken ct);}
public interface ICancelOrderUnitOfWork{Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct);}

public sealed class CancelOrderHandler{
 const string Operation="CancelOrder";
 readonly IOrderCancellationOrderRepository _orders;readonly ICancelOrderDeliveryRepository _deliveries;readonly ICancelOrderAuthorization _auth;readonly IIdempotencyStore _idem;readonly ICancelOrderUnitOfWork _uow;
 public CancelOrderHandler(IOrderCancellationOrderRepository orders,ICancelOrderDeliveryRepository deliveries,ICancelOrderAuthorization auth,IIdempotencyStore idem,ICancelOrderUnitOfWork uow){_orders=orders;_deliveries=deliveries;_auth=auth;_idem=idem;_uow=uow;}
 public async Task<CancelOrderResult> HandleAsync(CancelOrderCommand c,CancellationToken ct=default){
  if(c.OrderId==Guid.Empty||c.ActorId==Guid.Empty)throw new CancelOrderRejectedException("Order and Actor are required.");
  if(string.IsNullOrWhiteSpace(c.IdempotencyKey))throw new CancelOrderRejectedException("Idempotency key is required.");
  var fp=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{c.OrderId:N}|{c.ActorId:N}")));
  var existing=await _idem.GetAsync(c.ActorId,Operation,c.IdempotencyKey,ct);if(existing is not null)return Resolve(existing,fp);
  CancelOrderResult? result=null;
  await _uow.ExecuteAsync(async tx=>{
   var reserved=await _idem.ReserveAsync(c.ActorId,Operation,c.IdempotencyKey,fp,tx);if(reserved is not null){result=Resolve(reserved,fp);return;}
   // Cross-aggregate mutation lock order: Order first, then Delivery.
   var order=await _orders.GetAsync(c.OrderId,tx)??throw new CancelOrderRejectedException("Order was not found.");
   if(!await _auth.CanCancelAsync(c.ActorId,order,tx))throw new CancelOrderRejectedException("Actor is not authorized to cancel this Order.");
   var active=await _deliveries.GetActiveByOrderIdAsync(c.OrderId,tx);
   if(active is not null&&active.Status is DeliveryEntityStatus.PickedUp or DeliveryEntityStatus.OutForDelivery)throw new CancelOrderRejectedException("Order cancellation is not allowed after pickup.");
   try
   {
    order.Cancel();
    if(active is not null) active.CancelBeforePickup();
   }
   catch(DomainRuleViolationException exception)
   {
    throw new CancelOrderRejectedException(exception.Message);
   }\n   await _orders.SaveAsync(order,tx);
   if(active is not null) await _deliveries.SaveAsync(active,tx);
   result=new CancelOrderResult(order.Id,active?.Id);await _idem.CompleteAsync(c.ActorId,Operation,c.IdempotencyKey,new IdempotencyCompletion("OrderCancellation",result.OrderId,System.Text.Json.JsonSerializer.Serialize(result)),tx);
  },ct);
  return result??throw new InvalidOperationException("Cancellation completed without a result.");
 }
 static CancelOrderResult Resolve(IdempotencyRecord x,string fp){if(!string.Equals(x.Fingerprint,fp,StringComparison.Ordinal))throw new CancelOrderRejectedException("Idempotency key was already used with a different request.");if(x.Status!=IdempotencyStatus.Completed||!string.Equals(x.ResourceType,"OrderCancellation",StringComparison.Ordinal)||x.ResourceId is null||string.IsNullOrWhiteSpace(x.ResultPayload))throw new CancelOrderRejectedException("Idempotency record is incomplete.");var result=System.Text.Json.JsonSerializer.Deserialize<CancelOrderResult>(x.ResultPayload);if(result is null||result.OrderId==Guid.Empty||result.OrderId!=x.ResourceId.Value)throw new CancelOrderRejectedException("Idempotency record is invalid.");return result;}
}