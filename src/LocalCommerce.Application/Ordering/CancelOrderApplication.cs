using System.Security.Cryptography;
using System.Text;
using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus=LocalCommerce.Domain.Delivery.DeliveryStatus;
using OrderEntity=LocalCommerce.Domain.Ordering.Order;
using OrderStatus=LocalCommerce.Domain.Ordering.OrderStatus;

namespace LocalCommerce.Application.Ordering;

public sealed record CancelOrderCommand(Guid OrderId,Guid ActorId,string IdempotencyKey);
public sealed record CancelOrderResult(Guid OrderId,Guid? DeliveryId);
public sealed record CancelOrderIdempotencyRecord(string Key,string Fingerprint,CancelOrderResult Result);
public sealed class CancelOrderRejectedException:Exception{public CancelOrderRejectedException(string message):base(message){}}

public interface IOrderCancellationOrderRepository{Task<OrderEntity?> GetAsync(Guid orderId,CancellationToken ct);Task SaveAsync(OrderEntity order,CancellationToken ct);}
public interface ICancelOrderDeliveryRepository{Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId,CancellationToken ct);Task SaveAsync(DeliveryEntity delivery,CancellationToken ct);}
public interface ICancelOrderAuthorization{Task<bool> CanCancelAsync(Guid actorId,OrderEntity order,CancellationToken ct);}
public interface ICancelOrderIdempotencyStore{Task<CancelOrderIdempotencyRecord?> GetAsync(Guid actorId,string operation,string key,CancellationToken ct);Task<CancelOrderIdempotencyRecord?> ReserveAsync(Guid actorId,string operation,string key,string fingerprint,CancellationToken ct);Task CompleteAsync(Guid actorId,string operation,string key,CancelOrderResult result,CancellationToken ct);}
public interface ICancelOrderUnitOfWork{Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct);}

public sealed class CancelOrderHandler{
 const string Operation="CancelOrder";
 readonly IOrderCancellationOrderRepository _orders;readonly ICancelOrderDeliveryRepository _deliveries;readonly ICancelOrderAuthorization _auth;readonly ICancelOrderIdempotencyStore _idem;readonly ICancelOrderUnitOfWork _uow;
 public CancelOrderHandler(IOrderCancellationOrderRepository orders,ICancelOrderDeliveryRepository deliveries,ICancelOrderAuthorization auth,ICancelOrderIdempotencyStore idem,ICancelOrderUnitOfWork uow){_orders=orders;_deliveries=deliveries;_auth=auth;_idem=idem;_uow=uow;}
 public async Task<CancelOrderResult> HandleAsync(CancelOrderCommand c,CancellationToken ct=default){
  if(c.OrderId==Guid.Empty||c.ActorId==Guid.Empty)throw new CancelOrderRejectedException("Order and Actor are required.");
  if(string.IsNullOrWhiteSpace(c.IdempotencyKey))throw new CancelOrderRejectedException("Idempotency key is required.");
  var fp=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{c.OrderId:N}|{c.ActorId:N}")));
  var existing=await _idem.GetAsync(c.ActorId,Operation,c.IdempotencyKey,ct);if(existing is not null)return Resolve(existing,fp);
  var order=await _orders.GetAsync(c.OrderId,ct)??throw new CancelOrderRejectedException("Order was not found.");
  if(!await _auth.CanCancelAsync(c.ActorId,order,ct))throw new CancelOrderRejectedException("Actor is not authorized to cancel this Order.");
  var active=await _deliveries.GetActiveByOrderIdAsync(c.OrderId,ct);
  if(active is not null&&active.Status is DeliveryEntityStatus.PickedUp or DeliveryEntityStatus.OutForDelivery)throw new CancelOrderRejectedException("Order cancellation is not allowed after pickup.");
  CancelOrderResult? result=null;
  await _uow.ExecuteAsync(async tx=>{
   var reserved=await _idem.ReserveAsync(c.ActorId,Operation,c.IdempotencyKey,fp,tx);if(reserved is not null){result=Resolve(reserved,fp);return;}
   order=await _orders.GetAsync(c.OrderId,tx)??throw new CancelOrderRejectedException("Order was not found.");
   if(!await _auth.CanCancelAsync(c.ActorId,order,tx))throw new CancelOrderRejectedException("Actor is not authorized to cancel this Order.");
   active=await _deliveries.GetActiveByOrderIdAsync(c.OrderId,tx);
   if(active is not null&&active.Status is DeliveryEntityStatus.PickedUp or DeliveryEntityStatus.OutForDelivery)throw new CancelOrderRejectedException("Order cancellation is not allowed after pickup.");
   order.Cancel();await _orders.SaveAsync(order,tx);
   if(active is not null){active.CancelBeforePickup();await _deliveries.SaveAsync(active,tx);}
   result=new CancelOrderResult(order.Id,active?.Id);await _idem.CompleteAsync(c.ActorId,Operation,c.IdempotencyKey,result,tx);
  },ct);
  return result??throw new InvalidOperationException("Cancellation completed without a result.");
 }
 static CancelOrderResult Resolve(CancelOrderIdempotencyRecord x,string fp){if(x.Fingerprint!=fp)throw new CancelOrderRejectedException("Idempotency key was already used with a different request.");if(x.Result.OrderId==Guid.Empty)throw new CancelOrderRejectedException("Idempotency record is incomplete.");return x.Result;}
}