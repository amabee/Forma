namespace Forma.Core.Rendering;

public interface IBridge
{
    Task SendAsync(object message);

    event EventHandler<BridgeMessage>? MessageReceived;
}
