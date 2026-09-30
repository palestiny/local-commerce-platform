using LocalCommerce.Application.Ordering;
using LocalCommerce.Application.Delivery;
using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus=LocalCommerce.Domain.Delivery.DeliveryStatus;
using OrderEntity=LocalCommerce.Domain.Ordering.Order;
using OrderStatus=LocalCommerce.Domain.Ordering.OrderStatus;
using Xunit;

namespace LocalCommerce.Application.Tests.Ordering;

public sealed class CancelOrderApplicationTests
{
    [Fact] public async Task Cancels_order_and_active_delivery_before_pickup(){var f=Fixture.Create();var r=await f.Handler.HandleAsync(new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1"));Assert.Equal(OrderStatus.Cancelled,f.Order.Status);Assert.Equal(DeliveryEntityStatus.Cancelled,f.Delivery!.Status);Assert.Equal(f.Order.Id,r.OrderId);}
    [Fact] public async Task Rejects_cancellation_after_pickup(){var f=Fixture.Create();f.Delivery!.ConfirmPickup(f.DriverId);var act=()=>f.Handler.HandleAsync(new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1"));await Assert.ThrowsAsync<CancelOrderRejectedException>(act);Assert.NotEqual(OrderStatus.Cancelled,f.Order.Status);}
    [Fact] public async Task Same_key_replays(){var f=Fixture.Create();var c=new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1");var a=await f.Handler.HandleAsync(c);var b=await f.Handler.HandleAsync(c);Assert.Equal(a,b);}
    [Fact] public async Task Different_request_same_key_rejected(){var f=Fixture.Create();var c=new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1");await f.Handler.HandleAsync(c);var act=()=>f.Handler.HandleAsync(c with{OrderId=Guid.NewGuid()});await Assert.ThrowsAsync<CancelOrderRejectedException>(act);}
    [Fact] public async Task Order_without_delivery_can_be_cancelled(){var f=Fixture.Create(false);var r=await f.Handler.HandleAsync(new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1"));Assert.Equal(OrderStatus.Cancelled,f.Order.Status);Assert.Null(r.DeliveryId);}
    [Fact] public async Task Transaction_failure_does_not_apply_cancellation(){var f=Fixture.Create();f.Uow.FailBeforeOperation=true;var act=()=>f.Handler.HandleAsync(new CancelOrderCommand(f.Order.Id,f.ActorId,"cancel-1"));await Assert.ThrowsAsync<InvalidOperationException>(act);Assert.NotEqual(OrderStatus.Cancelled,f.Order.Status);Assert.Equal(DeliveryEntityStatus.Unassigned,f.Delivery!.Status);}
    private sealed class Fixture{
        public Guid ActorId{get;}=Guid.NewGuid(); public Guid DriverId{get;}=Guid.NewGuid(); public OrderEntity Order{get;} public DeliveryEntity? Delivery{get;} public FakeOrderRepo Orders{get;} public FakeDeliveryRepo Deliveries{get;} public FakeAuth Auth{get;} public FakeIdem Idem{get;} public FakeUow Uow{get;} public CancelOrderHandler Handler{get;}
        private Fixture(bool withDelivery){var store=Guid.NewGuid();var item=new LocalCommerce.Domain.Ordering.OrderItem(Guid.NewGuid(),store,"Milk",null,10m,1,0m,10m);Order=OrderEntity.Create(store,new[]{item});Order.SubmitForStoreConfirmation();Order.Accept();Order.Prepare();Order.MarkReadyForPickup();Delivery=withDelivery?DeliveryEntity.Create(Order.Id,store):null;if(Delivery is not null){} Orders=new(Order);Deliveries=new(Delivery);Auth=new();Idem=new();Uow=new();Handler=new(Orders,Deliveries,Auth,Idem,Uow);}
        public static Fixture Create(bool withDelivery=true)=>new(withDelivery);
    }
    private sealed class FakeOrderRepo:IOrderCancellationOrderRepository{private readonly OrderEntity o;public FakeOrderRepo(OrderEntity x)=>o=x;public Task<OrderEntity?> GetAsync(Guid id,CancellationToken ct)=>Task.FromResult<OrderEntity?>(o.Id==id?o:null);public Task SaveAsync(OrderEntity order,CancellationToken ct)=>Task.CompletedTask;}
    private sealed class FakeDeliveryRepo:ICancelOrderDeliveryRepository{private readonly DeliveryEntity? d;public FakeDeliveryRepo(DeliveryEntity? x)=>d=x;public Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid id,CancellationToken ct)=>Task.FromResult(d is not null&&d.OrderId==id&&d.Status is not DeliveryEntityStatus.Failed and not DeliveryEntityStatus.Cancelled and not DeliveryEntityStatus.Delivered?d:null);public Task SaveAsync(DeliveryEntity x,CancellationToken ct)=>Task.CompletedTask;}
    private sealed class FakeAuth:ICancelOrderAuthorization{public Task<bool> CanCancelAsync(Guid actorId,OrderEntity order,CancellationToken ct)=>Task.FromResult(true);}
    private sealed class FakeIdem:ICancelOrderIdempotencyStore{readonly Dictionary<string,CancelOrderIdempotencyRecord> d=new();public Task<CancelOrderIdempotencyRecord?> GetAsync(Guid a,string o,string k,CancellationToken ct)=>Task.FromResult(d.TryGetValue(k,out var x)?x:null);public Task<CancelOrderIdempotencyRecord?> ReserveAsync(Guid a,string o,string k,string fp,CancellationToken ct){if(d.TryGetValue(k,out var x))return Task.FromResult<CancelOrderIdempotencyRecord?>(x);d[k]=new(k,fp,new CancelOrderResult(Guid.Empty,null));return Task.FromResult<CancelOrderIdempotencyRecord?>(null);}public Task CompleteAsync(Guid a,string o,string k,CancelOrderResult r,CancellationToken ct){d[k]=d[k] with{Result=r};return Task.CompletedTask;}}
    private sealed class FakeUow:ICancelOrderUnitOfWork{public bool FailBeforeOperation{get;set;}public Task ExecuteAsync(Func<CancellationToken,Task> op,CancellationToken ct){if(FailBeforeOperation)throw new InvalidOperationException("Simulated transaction failure.");return op(ct);}}
}