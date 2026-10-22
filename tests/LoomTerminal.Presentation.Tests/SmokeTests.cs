namespace LoomTerminal.Presentation.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_internals_are_reachable_from_Presentation()
    {
        Assert.True(CoreFactory.CreateAndGetColumns() > 0);
    }
}
