using Forma.WebView2;
using Microsoft.Web.WebView2.Core;

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

        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Web", "index.html");

        var navigationCompleted = new TaskCompletionSource<bool>();

        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
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

        // Bridge is created after navigation so the page is listening before
        // the first command is sent.
        _bridge = new WebView2Bridge(_webView.CoreWebView2);

        // Create our Forma renderer
        var renderer = new WebView2Renderer(_bridge);

        var form = new Forma.Core.Form { Title = "Hello Forma" };
        var panel = new Forma.Core.Controls.Panel();
        var title = new Forma.Core.Controls.Label { Text = "My first Forma form" };
        var nameInput = new Forma.Core.Controls.TextBox { Id = "name", Text = "" };
        var greeting = new Forma.Core.Controls.Label { Text = "Type your name, then say hello." };

        var button = new Forma.Core.Controls.Button
        {
            Id = "hello",
            Text = "Say hello",
            Name = "btnHello",
        };

        button.Click += (_, _) =>
        {
            var name = nameInput.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Hello, friend!");
                return;
            }

            MessageBox.Show("Hello, " + name + "!");
        };

        panel.Add(title);
        panel.Add(nameInput);
        panel.Add(button);
        panel.Add(greeting);
        form.Add(panel);

        // This confirms browser input reaches C# while the greeting handler
        // above remains yours to implement.
        nameInput.TextChanged += (_, _) => Text = $"Forma Demo — {nameInput.Text}";

        // Initialize renderer
        await renderer.InitializeAsync();

        // Render the control
        await renderer.RenderAsync(form);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _bridge?.Dispose();

        base.OnFormClosed(e);
    }
}
