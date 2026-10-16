using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class FailDeliveryHandlerTests
{
    [Fact]
    public async Task Authorized_actor_can_fail_active_delivery()
    {
        var fixture=Fixture.Create();
        var result=await fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"DRIVER_UNAVAILABLE","Driver became unavailable.","fail-1"));
        Assert.Equal(fixture.Delivery.Id,result.DeliveryId);
        Assert.Equal(DeliveryEntityStatus.Failed,fixture.Delivery.Status);
        Assert.Equal("DRIVER_UNAVAILABLE",fixture.Delivery.FailureCode);
        Assert.NotNull(fixture.Delivery.FailedAt);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_fail_delivery()
    {
        var fixture=Fixture.Create(); fixture.Authorization.Allowed=false;
        var act=()=>fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","Reason","fail-1"));
        var error=await Assert.ThrowsAsync<FailDeliveryRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.AuthorizationForbidden,error.Code);
        Assert.NotEqual(DeliveryEntityStatus.Failed,fixture.Delivery.Status);
    }

    [Fact]
    public async Task Failure_requires_non_terminal_delivery()
    {
        var fixture=Fixture.Create(); fixture.Delivery=DeliveryEntity.Create(Guid.NewGuid(),Guid.NewGuid()); fixture.Delivery.AssignDriver(fixture.ActorId); fixture.Delivery.ConfirmPickup(fixture.ActorId); fixture.Delivery.StartDelivery(); fixture.Delivery.Complete(); fixture.Repository.SetDelivery(fixture.Delivery);
        var act=()=>fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","Reason","fail-1"));
        var error=await Assert.ThrowsAsync<FailDeliveryRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.DeliveryInvalidState,error.Code);
        Assert.Equal(DeliveryEntityStatus.Delivered,fixture.Delivery.Status);
    }

    [Fact]
    public async Task Failure_requires_code_and_reason()
    {
        var fixture=Fixture.Create();
        await Assert.ThrowsAsync<FailDeliveryRejectedException>(()=>fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"","Reason","fail-1")));
        await Assert.ThrowsAsync<FailDeliveryRejectedException>(()=>fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","","fail-2")));
        Assert.NotEqual(DeliveryEntityStatus.Failed,fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_replays_original_result()
    {
        var fixture=Fixture.Create(); var command=new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","Reason","fail-1");
        var first=await fixture.Handler.HandleAsync(command); var second=await fixture.Handler.HandleAsync(command);
        Assert.Equal(first,second); Assert.Equal(DeliveryEntityStatus.Failed,fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_request_is_rejected()
    {
        var fixture=Fixture.Create(); var command=new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","Reason","fail-1"); await fixture.Handler.HandleAsync(command);
        var act=()=>fixture.Handler.HandleAsync(command with { FailureCode="Y" });
        var error=await Assert.ThrowsAsync<FailDeliveryRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.IdempotencyKeyReused,error.Code);
    }

    [Fact]
    public async Task Unit_of_work_failure_leaves_delivery_unchanged()
    {
        var fixture=Fixture.Create(); fixture.UnitOfWork.FailBeforeOperation=true;
        var act=()=>fixture.Handler.HandleAsync(new FailDeliveryCommand(fixture.Delivery.Id,fixture.ActorId,"X","Reason","fail-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(act); Assert.Equal(DeliveryEntityStatus.OutForDelivery,fixture.Delivery.Status);
    }

    private sealed class Fixture
    {
        private Fixture(DeliveryEntity d,Guid actor){Delivery=d;ActorId=actor;Repository=new(d);Authorization=new();IdempotencyStore=new();UnitOfWork=new();Handler=new(Repository,Authorization,IdempotencyStore,UnitOfWork);}
        public DeliveryEntity Delivery{get;set;} public Guid ActorId{get;} public FakeDeliveryRepository Repository{get;} public FakeAuthorization Authorization{get;} public FakeIdempotencyStore IdempotencyStore{get;} public FakeUnitOfWork UnitOfWork{get;} public FailDeliveryHandler Handler{get;}
        public static Fixture Create(){var actor=Guid.NewGuid();var d=DeliveryEntity.Create(Guid.NewGuid(),Guid.NewGuid());d.AssignDriver(actor);d.ConfirmPickup(actor);d.StartDelivery();return new(d,actor);}
    }
    private sealed class FakeDeliveryRepository:IFailDeliveryDeliveryRepository{private DeliveryEntity _d;public FakeDeliveryRepository(DeliveryEntity d)=>_d=d;public Task<DeliveryEntity?> GetAsync(Guid id,CancellationToken ct)=>Task.FromResult<DeliveryEntity?>(_d.Id==id?_d:null);public void SetDelivery(DeliveryEntity d)=>_d=d;public Task SaveAsync(DeliveryEntity d,CancellationToken ct)=>Task.CompletedTask;}
    private sealed class FakeAuthorization:IFailDeliveryAuthorization{public bool Allowed{get;set;}=true;public Task<bool> CanFailDeliveryAsync(Guid actorId,DeliveryEntity d,CancellationToken ct)=>Task.FromResult(Allowed);}
    private sealed class FakeIdempotencyStore : IIdempotencyStore
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
    private sealed class FakeUnitOfWork:IFailDeliveryUnitOfWork{public bool FailBeforeOperation{get;set;}public Task ExecuteAsync(Func<CancellationToken,Task> op,CancellationToken ct){if(FailBeforeOperation)throw new InvalidOperationException("Simulated transaction failure.");return op(ct);}}
}
