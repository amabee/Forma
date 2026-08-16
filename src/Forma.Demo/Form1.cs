using System.Text.Json;
using Forma.Core.Controls;
using Forma.WebView2;
using Microsoft.Web.WebView2.WinForms;

namespace Forma.Demo;

public class Form1 : Form
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webView;
    private WebView2Bridge? _bridge;

    public Form1()
    {
        Text = "Forma Demo";
        ClientSize = new Size(1366, 768);

        StartPosition = FormStartPosition.CenterScreen;

        _webView = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };

        Controls.Add(_webView);

        Load += Form1_Load;
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        await _webView.EnsureCoreWebView2Async();

        _bridge = new WebView2Bridge(_webView);

        _bridge.MessageReceived += (_, message) =>
        {
            MessageBox.Show(JsonSerializer.Serialize(message), "Forma Bridge");
        };

        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Web", "index.html");

        var navigationCompleted = new TaskCompletionSource<bool>();

        void OnNavigationCompleted(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e
        )
        {
            navigationCompleted.TrySetResult(e.IsSuccess);
        }

        _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

        _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);

        var loaded = await navigationCompleted.Task;

        _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;

        if (!loaded)
        {
            MessageBox.Show("Forma Web runtime failed to load.");

            return;
        }

        // 5. Create our Forma renderer
        var renderer = new WebView2Renderer(_bridge);

        // 6. Create a Forma control
        var button = new Forma.Core.Controls.Button
        {
            Id = "hello",
            Text = "Hello Forma!",
            Name = "btnHello",
        };

        // 7. Initialize renderer
        await renderer.InitializeAsync();

        // 8. Render the control
        await renderer.RenderAsync(button);
    }
}
