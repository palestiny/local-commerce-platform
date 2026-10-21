using LocalCommerce.Domain;
using LocalCommerce.Domain.Delivery;
using Xunit;

namespace LocalCommerce.Domain.Tests.Delivery;

public sealed class DriverTests
{
    [Fact]
    public void New_driver_is_active_by_default()
    {
        var driver = Driver.Create(Guid.NewGuid());

        Assert.True(driver.IsActive);
    }

    [Fact]
    public void Driver_requires_identity()
    {
        Assert.Throws<DomainRuleViolationException>(() => Driver.Create(Guid.Empty));
    }

    [Fact]
    public void Inactive_driver_cannot_be_reactivated_without_explicit_command()
    {
        var driver = Driver.Create(Guid.NewGuid());

        driver.Deactivate();

        Assert.False(driver.IsActive);
    }

    [Fact]
    public void Active_driver_can_be_deactivated()
    {
        var driver = Driver.Create(Guid.NewGuid());

        driver.Deactivate();

        Assert.False(driver.IsActive);
    }
}
