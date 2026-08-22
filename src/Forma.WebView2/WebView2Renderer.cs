using System.ComponentModel;
using Forma.Core.Controls;
using Forma.Core.Rendering;

namespace Forma.WebView2;

/// <summary>
/// Projects a Forma control tree onto the web runtime and keeps it in sync as
/// the tree changes.
/// </summary>
public sealed class WebView2Renderer : IRenderer
{
    private readonly IBridge _bridge;

    /// <summary>
    /// Controls currently projected into the web runtime, keyed by
    /// <see cref="Control.Id"/>. Holds the handler delegates too, so every
    /// subscription made at render time can be undone at removal time.
    /// </summary>
    private readonly Dictionary<string, Registration> _controls = [];

    public WebView2Renderer(IBridge bridge)
    {
        _bridge = bridge;

        _bridge.MessageReceived += OnMessageReceived;
    }

    private sealed class Registration
    {
        public required Control Control { get; init; }
        public required PropertyChangedEventHandler PropertyChanged { get; init; }
        public required EventHandler<ControlEventArgs> ChildAdded { get; init; }
        public required EventHandler<ControlEventArgs> ChildRemoved { get; init; }
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders <paramref name="control"/> and its subtree, parents before
    /// children so the runtime can attach each element to an existing node.
    /// </summary>
    public async Task RenderAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        // Rendering an already-rendered control must not subscribe a second
        // time, or every later change would be sent once per render.
        if (_controls.ContainsKey(control.Id))
        {
            return;
        }

        Register(control);

        await _bridge.SendAsync(
            new
            {
                type = "create",
                id = control.Id,
                control = control.ControlType,
                parentId = control.Parent?.Id,
                properties = new { text = control.Text },
            }
        );

        foreach (var child in control.Children)
        {
            await RenderAsync(child);
        }
    }

    public Task UpdateAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return _bridge.SendAsync(
            new
            {
                type = "update",
                id = control.Id,
                properties = new { text = control.Text },
            }
        );
    }

    /// <summary>
    /// Removes <paramref name="control"/> and its subtree.
    /// </summary>
    /// <remarks>
    /// One message is sent for the root: removing its element detaches the
    /// descendants with it. The descendants still have to be unregistered here
    /// so their subscriptions do not outlive the elements.
    /// </remarks>
    public Task RemoveAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (!_controls.ContainsKey(control.Id))
        {
            return Task.CompletedTask;
        }

        Unregister(control);

        return _bridge.SendAsync(new { type = "remove", id = control.Id });
    }

    private void Register(Control control)
    {
        var registration = new Registration
        {
            Control = control,
            PropertyChanged = (_, _) => Observe(UpdateAsync(control)),
            ChildAdded = (_, e) => Observe(RenderAsync(e.Control)),
            ChildRemoved = (_, e) => Observe(RemoveAsync(e.Control)),
        };

        control.PropertyChanged += registration.PropertyChanged;
        control.ChildAdded += registration.ChildAdded;
        control.ChildRemoved += registration.ChildRemoved;

        _controls[control.Id] = registration;
    }

    private void Unregister(Control control)
    {
        foreach (var child in control.Children)
        {
            Unregister(child);
        }

        if (!_controls.Remove(control.Id, out var registration))
        {
            return;
        }

        control.PropertyChanged -= registration.PropertyChanged;
        control.ChildAdded -= registration.ChildAdded;
        control.ChildRemoved -= registration.ChildRemoved;
    }

    /// <summary>
    /// Surfaces failures from sends started by an event handler, which has no
    /// caller left to await it.
    /// </summary>
    private static void Observe(Task task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        task.ContinueWith(
            completed => Console.WriteLine(
                $"Forma renderer error: {completed.Exception?.GetBaseException().Message}"
            ),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        );
    }

    private void OnMessageReceived(object? sender, BridgeMessage message)
    {
        if (message.Type != "event")
            return;

        if (message.Id is null)
            return;

        if (!_controls.TryGetValue(message.Id, out var registration))
        {
            return;
        }

        if (registration.Control is Button button && message.Event == "click")
        {
            button.OnClick();
        }
    }
}
