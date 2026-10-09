using LocalCommerce.Application.Delivery; using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery; using DeliveryEntityStatus=LocalCommerce.Domain.Delivery.DeliveryStatus; using Xunit;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
namespace LocalCommerce.Application.Tests.Delivery;
public sealed class CreateReplacementDeliveryHandlerTests{
 [Fact] public async Task Failed_delivery_can_create_new_unassigned_replacement_when_order_eligible(){var f=Fixture.Create();var old=f.Delivery;old.Fail("DRIVER_UNAVAILABLE","Driver unavailable.");var r=await f.Handler.HandleAsync(new CreateReplacementDeliveryCommand(old.OrderId,old.StoreId,f.ActorId,"rep-1"));Assert.NotEqual(old.Id,r.DeliveryId);Assert.Equal(DeliveryEntityStatus.Unassigned,f.Repository.Get(r.DeliveryId)!.Status);Assert.Equal(DeliveryEntityStatus.Failed,old.Status);}
 [Fact] public async Task Replacement_rejected_when_store_does_not_match_order_store(){var f=Fixture.Create();f.Delivery.Fail("X","Reason");var act=()=>f.Handler.HandleAsync(new CreateReplacementDeliveryCommand(f.Delivery.OrderId,Guid.NewGuid(),f.ActorId,"rep-mismatch"));var error=await Assert.ThrowsAsync<CreateReplacementDeliveryRejectedException>(act);Assert.Equal(ApplicationErrorCodes.RequestInvalid,error.Code);Assert.Null(f.Repository.GetActiveByOrderId(f.OrderId));}
 [Fact] public async Task Replacement_requires_no_active_delivery(){var f=Fixture.Create();var act=()=>f.Handler.HandleAsync(new CreateReplacementDeliveryCommand(f.Delivery.OrderId,f.Delivery.StoreId,f.ActorId,"rep-1"));var error=await Assert.ThrowsAsync<CreateReplacementDeliveryRejectedException>(act);Assert.Equal(ApplicationErrorCodes.DeliveryInvalidState,error.Code);}
 [Fact] public async Task Replacement_rejected_when_order_not_eligible(){var f=Fixture.Create();f.Delivery.Fail("X","Reason");f.OrderEligible=false;var act=()=>f.Handler.HandleAsync(new CreateReplacementDeliveryCommand(f.Delivery.OrderId,f.Delivery.StoreId,f.ActorId,"rep-1"));var error=await Assert.ThrowsAsync<CreateReplacementDeliveryRejectedException>(act);Assert.Equal(ApplicationErrorCodes.OrderInvalidState,error.Code);}
 [Fact] public async Task Same_idempotency_key_replays(){var f=Fixture.Create();f.Delivery.Fail("X","Reason");var c=new CreateReplacementDeliveryCommand(f.Delivery.OrderId,f.Delivery.StoreId,f.ActorId,"rep-1");var a=await f.Handler.HandleAsync(c);var b=await f.Handler.HandleAsync(c);Assert.Equal(a,b);}
 [Fact] public async Task Different_request_same_key_rejected(){var f=Fixture.Create();f.Delivery.Fail("X","Reason");var c=new CreateReplacementDeliveryCommand(f.Delivery.OrderId,f.Delivery.StoreId,f.ActorId,"rep-1");await f.Handler.HandleAsync(c);var act=()=>f.Handler.HandleAsync(c with{StoreId=Guid.NewGuid()});var error=await Assert.ThrowsAsync<CreateReplacementDeliveryRejectedException>(act);Assert.Equal(ApplicationErrorCodes.IdempotencyKeyReused,error.Code);}
 private sealed class Fixture{public Guid ActorId{get;}=Guid.NewGuid();public Guid OrderId{get;}=Guid.NewGuid();public Guid StoreId{get;}=Guid.NewGuid();public DeliveryEntity Delivery{get;}public FakeRepo Repository{get;}public FakeOrderLock OrderLock{get;}public FakeEligibility Eligibility{get;}public FakeIdempotency Idempotency{get;}public FakeUow Uow{get;}public CreateReplacementDeliveryHandler Handler{get;}public bool OrderEligible{get=>Eligibility.Value;set=>Eligibility.Value=value;} private Fixture(){Delivery=DeliveryEntity.Create(OrderId,StoreId);Repository=new(Delivery);OrderLock=new(StoreId);Eligibility=new();Idempotency=new();Uow=new();Handler=new(Repository,OrderLock,Eligibility,Idempotency,Uow);}public static Fixture Create()=>new();}
 private sealed class FakeRepo:ICreateReplacementDeliveryRepository{readonly Dictionary<Guid,DeliveryEntity> d=new();public FakeRepo(DeliveryEntity x){d[x.Id]=x;}public Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId,CancellationToken ct)=>Task.FromResult<DeliveryEntity?>(d.Values.FirstOrDefault(x=>x.OrderId==orderId&&x.Status!=DeliveryEntityStatus.Failed));public Task AddAsync(DeliveryEntity x,CancellationToken ct){d[x.Id]=x;return Task.CompletedTask;}public DeliveryEntity? Get(Guid id)=>d.TryGetValue(id,out var x)?x:null;public DeliveryEntity? GetActiveByOrderId(Guid orderId)=>d.Values.FirstOrDefault(x=>x.OrderId==orderId&&(x.Status==DeliveryEntityStatus.Unassigned||x.Status==DeliveryEntityStatus.Assigned||x.Status==DeliveryEntityStatus.PickedUp||x.Status==DeliveryEntityStatus.OutForDelivery));}
 private sealed class FakeOrderLock(Guid storeId):IReplacementDeliveryOrderLock{public Task<Guid?> LockOrderForMutationAsync(Guid orderId,CancellationToken ct)=>Task.FromResult<Guid?>(storeId);}
 private sealed class FakeEligibility:IReplacementDeliveryEligibility{public bool Value{get;set;}=true;public Task<bool> IsOrderEligibleAsync(Guid orderId,CancellationToken ct)=>Task.FromResult(Value);}
 private sealed class FakeIdempotency : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotencyRecord> _records = new();
        private static string Key(Guid scopeId, string operation, string key) => $"{scopeId:N}|{operation}|{key}";
        public Task<IdempotencyRecord?> GetAsync(Guid scopeId,string operation,string key,CancellationToken ct) =>
            Task.FromResult(_records.TryGetValue(Key(scopeId,operation,key),out var x)?x:null);
        public Task<IdempotencyRecord?> ReserveAsync(Guid scopeId,string operation,string key,string fingerprint,CancellationToken ct)
        {
            var k=Key(scopeId,operation,key);
            if(_records.TryGetValue(k,out var x)) return Task.FromResult<IdempotencyRecord?>(x);
            _records[k]=new IdempotencyRecord(scopeId,operation,key,fingerprint,IdempotencyStatus.Reserved,null,null,null);
            return Task.FromResult<IdempotencyRecord?>(null);
        }
        public Task CompleteAsync(Guid scopeId,string operation,string key,IdempotencyCompletion completion,CancellationToken ct)
        {
            var k=Key(scopeId,operation,key); var x=_records[k];
            _records[k]=x with { Status=IdempotencyStatus.Completed, ResourceType=completion.ResourceType, ResourceId=completion.ResourceId, ResultPayload=completion.ResultPayload };
            return Task.CompletedTask;
        }
    }
 private sealed class FakeUow:ICreateReplacementDeliveryUnitOfWork{public Task ExecuteAsync(Func<CancellationToken,Task> op,CancellationToken ct)=>op(ct);}
}