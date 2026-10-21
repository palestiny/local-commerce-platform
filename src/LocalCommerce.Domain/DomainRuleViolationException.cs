namespace LocalCommerce.Domain;

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
}
