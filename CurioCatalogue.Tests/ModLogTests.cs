using NSubstitute;
using Vintagestory.API.Common;

namespace CurioCatalogue.Tests;

// ModLog.DebugEnabled is static: keep every test that touches it in this one class, which xUnit runs sequentially.
public class ModLogTests : IDisposable
{
    private readonly ICoreAPI api = Substitute.For<ICoreAPI>();
    private readonly ILogger logger = Substitute.For<ILogger>();
    private int evaluated;

    public ModLogTests()
    {
        api.Logger.Returns(logger);
        api.Side.Returns(EnumAppSide.Server);
    }

    public void Dispose() => ModLog.DebugEnabled = false;

    private int Expensive() => ++evaluated;

    [Fact]
    public void Disabled_debug_neither_logs_nor_evaluates_its_message()
    {
        ModLog.DebugEnabled = false;
        ModLog.Debug(api, $"value {Expensive()}");

        Assert.Equal(0, evaluated);
        logger.DidNotReceiveWithAnyArgs().Notification(default!, default!);
    }

    [Fact]
    public void Enabled_debug_logs_the_text_as_an_argument()
    {
        ModLog.DebugEnabled = true;
        ModLog.Debug(api, $"value {Expensive()} {{braces}}");

        Assert.Equal(1, evaluated);
        // The text goes in as an argument, never as the format string: attribute JSON has braces.
        logger.Received(1).Notification("[CurioCatalogue] {0} debug: {1}", EnumAppSide.Server, "value 1 {braces}");
    }
}
