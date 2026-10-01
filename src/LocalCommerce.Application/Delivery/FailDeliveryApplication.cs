using System.Security.Cryptography; using System.Text; using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery; using DeliveryEntityStatus=LocalCommerce.Domain.Delivery.DeliveryStatus;
namespace LocalCommerce.Application.Delivery;
public sealed record FailDeliveryCommand(Guid DeliveryId,Guid ActorId,string FailureCode,string FailureReason,string IdempotencyKey);
public sealed record FailDeliveryResult(Guid DeliveryId);
public sealed record FailDeliveryIdempotencyRecord(string Key,string Fingerprint,FailDeliveryResult Result);
public sealed class FailDeliveryRejectedException:Exception{public FailDeliveryRejectedException(string message):base(message){}}
public interface IFailDeliveryDeliveryRepository{Task<DeliveryEntity?> GetAsync(Guid id,CancellationToken ct);Task SaveAsync(DeliveryEntity d,CancellationToken ct);}
public interface IFailDeliveryAuthorization{Task<bool> CanFailDeliveryAsync(Guid actorId,DeliveryEntity d,CancellationToken ct);}
public interface IFailDeliveryIdempotencyStore{Task<FailDeliveryIdempotencyRecord?> GetAsync(Guid a,string o,string k,CancellationToken ct);Task<FailDeliveryIdempotencyRecord?> ReserveAsync(Guid a,string o,string k,string fp,CancellationToken ct);Task CompleteAsync(Guid a,string o,string k,FailDeliveryResult result,CancellationToken ct);}
public interface IFailDeliveryUnitOfWork{Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct);}
public sealed class FailDeliveryHandler{
 const string Operation="FailDelivery"; readonly IFailDeliveryDeliveryRepository _d;readonly IFailDeliveryAuthorization _a;readonly IFailDeliveryIdempotencyStore _i;readonly IFailDeliveryUnitOfWork _u;
 public FailDeliveryHandler(IFailDeliveryDeliveryRepository d,IFailDeliveryAuthorization a,IFailDeliveryIdempotencyStore i,IFailDeliveryUnitOfWork u){_d=d;_a=a;_i=i;_u=u;}
 public async Task<FailDeliveryResult> HandleAsync(FailDeliveryCommand c,CancellationToken ct=default){
  if(c.DeliveryId==Guid.Empty||c.ActorId==Guid.Empty)throw new FailDeliveryRejectedException("Delivery and Actor are required.");if(string.IsNullOrWhiteSpace(c.IdempotencyKey))throw new FailDeliveryRejectedException("Idempotency key is required.");if(string.IsNullOrWhiteSpace(c.FailureCode)||string.IsNullOrWhiteSpace(c.FailureReason))throw new FailDeliveryRejectedException("Failure code and reason are required.");
  var fp=Fingerprint(c);var ex=await _i.GetAsync(c.ActorId,Operation,c.IdempotencyKey,ct);if(ex is not null)return Resolve(ex,fp);
  FailDeliveryResult? result=null;await _u.ExecuteAsync(async tx=>{var reserved=await _i.ReserveAsync(c.ActorId,Operation,c.IdempotencyKey,fp,tx);if(reserved is not null){result=Resolve(reserved,fp);return;}var d=await _d.GetAsync(c.DeliveryId,tx)??throw new FailDeliveryRejectedException("Delivery was not found.");if(d.Status is DeliveryEntityStatus.Delivered or DeliveryEntityStatus.Failed)throw new FailDeliveryRejectedException("Terminal Delivery cannot be failed.");if(!await _a.CanFailDeliveryAsync(c.ActorId,d,tx))throw new FailDeliveryRejectedException("Actor is not authorized to fail delivery.");d.Fail(c.FailureCode,c.FailureReason);await _d.SaveAsync(d,tx);result=new FailDeliveryResult(d.Id);await _i.CompleteAsync(c.ActorId,Operation,c.IdempotencyKey,result,tx);},ct);return result??throw new InvalidOperationException("Delivery failure completed without a result.");
 }
 static string Fingerprint(FailDeliveryCommand c)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{c.DeliveryId:N}|{c.ActorId:N}|{c.FailureCode}|{c.FailureReason}")));
 static FailDeliveryResult Resolve(FailDeliveryIdempotencyRecord x,string fp){if(x.Fingerprint!=fp)throw new FailDeliveryRejectedException("Idempotency key was already used with a different request.");if(x.Result.DeliveryId==Guid.Empty)throw new FailDeliveryRejectedException("Idempotency record is incomplete.");return x.Result;}
}