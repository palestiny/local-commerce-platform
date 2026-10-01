using System.Security.Cryptography;
using LocalCommerce.Application.Idempotency;
using System.Text;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
namespace LocalCommerce.Application.Delivery;
public sealed record CompleteDeliveryCommand(Guid DeliveryId, Guid ActorId, string IdempotencyKey);
public sealed record CompleteDeliveryResult(Guid DeliveryId, Guid DriverId);public sealed class CompleteDeliveryRejectedException:Exception{public CompleteDeliveryRejectedException(string message):base(message){}}
public interface ICompleteDeliveryDeliveryRepository{Task<DeliveryEntity?> GetAsync(Guid id,CancellationToken ct);Task SaveAsync(DeliveryEntity delivery,CancellationToken ct);}
public interface ICompleteDeliveryAuthorization{Task<bool> CanCompleteDeliveryAsync(Guid actorId,DeliveryEntity delivery,CancellationToken ct);}
public interface ICompleteDeliveryUnitOfWork{Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct);}
public sealed class CompleteDeliveryHandler
{
 private const string Operation="CompleteDelivery"; private readonly ICompleteDeliveryDeliveryRepository _deliveries; private readonly ICompleteDeliveryAuthorization _authorization; private readonly ICompleteDeliveryIdempotencyStore _idempotency; private readonly ICompleteDeliveryUnitOfWork _unitOfWork;
 public CompleteDeliveryHandler(ICompleteDeliveryDeliveryRepository deliveries,ICompleteDeliveryAuthorization authorization,ICompleteDeliveryIdempotencyStore idempotency,ICompleteDeliveryUnitOfWork unitOfWork){_deliveries=deliveries;_authorization=authorization;_idempotency=idempotency;_unitOfWork=unitOfWork;}
 public async Task<CompleteDeliveryResult> HandleAsync(CompleteDeliveryCommand command,CancellationToken ct=default){
  if(command.DeliveryId==Guid.Empty)throw new CompleteDeliveryRejectedException("DeliveryId is required."); if(command.ActorId==Guid.Empty)throw new CompleteDeliveryRejectedException("ActorId is required."); if(string.IsNullOrWhiteSpace(command.IdempotencyKey))throw new CompleteDeliveryRejectedException("IdempotencyKey is required.");
  var fp=Fingerprint(command); var existing=await _idempotency.GetAsync(command.ActorId,Operation,command.IdempotencyKey,ct); if(existing is not null)return Resolve(existing,fp);
  CompleteDeliveryResult? result=null;
  await _unitOfWork.ExecuteAsync(async tx=>{var reserved=await _idempotency.ReserveAsync(command.ActorId,Operation,command.IdempotencyKey,fp,tx);if(reserved is not null){result=Resolve(reserved,fp);return;}var delivery=await _deliveries.GetAsync(command.DeliveryId,tx)??throw new CompleteDeliveryRejectedException("Delivery was not found.");if(delivery.Status!=DeliveryEntityStatus.OutForDelivery)throw new CompleteDeliveryRejectedException("Delivery must be OUT_FOR_DELIVERY.");if(!await _authorization.CanCompleteDeliveryAsync(command.ActorId,delivery,tx))throw new CompleteDeliveryRejectedException("Actor is not authorized to complete delivery.");var driverId=delivery.DriverId;if(driverId is null||driverId==Guid.Empty)throw new CompleteDeliveryRejectedException("Delivery must have an assigned driver.");delivery.Complete();await _deliveries.SaveAsync(delivery,tx);result=new CompleteDeliveryResult(delivery.Id,driverId.Value);await _idempotency.CompleteAsync(command.ActorId,Operation,command.IdempotencyKey,new IdempotencyCompletion("Delivery", result.DeliveryId, System.Text.Json.JsonSerializer.Serialize(result)),,tx);},ct);
  return result??throw new InvalidOperationException("Delivery completion completed without a result.");
 }
 private static CompleteDeliveryResult Resolve(IdempotencyRecord existing,string fp)
 {
  if(!string.Equals(existing.Fingerprint,fp,StringComparison.Ordinal))throw new CompleteDeliveryRejectedException("Idempotency key was already used with a different request.");
  if(existing.Status!=IdempotencyStatus.Completed||!string.Equals(existing.ResourceType,"Delivery",StringComparison.Ordinal)||existing.ResourceId is null||string.IsNullOrWhiteSpace(existing.ResultPayload))throw new CompleteDeliveryRejectedException("Idempotency record is incomplete.");
  var result=System.Text.Json.JsonSerializer.Deserialize<CompleteDeliveryResult>(existing.ResultPayload);
  if(result is null||result.DeliveryId==Guid.Empty||result.DriverId==Guid.Empty||result.DeliveryId!=existing.ResourceId.Value)throw new CompleteDeliveryRejectedException("Idempotency record is invalid.");
  return result;
 }
 private static string Fingerprint(CompleteDeliveryCommand c)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{c.DeliveryId:N}|{c.ActorId:N}")));
}
