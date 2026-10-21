using LocalCommerce.Domain;

namespace LocalCommerce.Domain.Delivery;

public sealed class Driver
{
    private Driver(Guid identityId)
    {
        Id = identityId;
        IsActive = true;
    }

    public Guid Id { get; }
    public bool IsActive { get; private set; }

    public static Driver Create(Guid identityId)
    {
        if (identityId == Guid.Empty)
            throw new DomainRuleViolationException("A Driver requires an identity.");

        return new Driver(identityId);
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
