using System.Reflection;
using System.Text.Json;
using Forma.Builder;
using Microsoft.Web.WebView2.WinForms;

// Integration regression: open the docked editor through a real WebView2
// message, not a direct method call, and verify its browser finishes loading.
internal static class Program
{
    private static object? Field(object instance, string name) => instance.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var builder = new BuilderWindow {
            StartPosition = FormStartPosition.Manual, Location = new(-30000, -30000),
            ShowInTaskbar = false
        };
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var sent = false; var checking = false; var passed = false; string? phase = null;
        timer.Tick += (_, _) => {
            if (checking) return;
            checking = true;
            try {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Docked code editor did not become ready.");
                if (!sent && Field(builder, "_ready") is true) {
                    Console.WriteLine("Designer ready; requesting docked editor.");
                    var model = (BuilderViewModel)Field(builder, "_viewModel")!;
                    model.ExecuteEdit("drop", model.Form!.Id, JsonSerializer.SerializeToElement(new { control = "button", x = 10, y = 10 }));
                    sent = true;
                    var web = (WebView2)Field(builder, "_preview")!;
                    // Return from the timer before injecting the command so its
                    // watchdog keeps ticking inside the modal message loop.
                    builder.BeginInvoke(new Action(() => {
                        if (args.Contains("--light")) _ = web.CoreWebView2.ExecuteScriptAsync("if(document.documentElement.dataset.theme !== 'light') document.getElementById('theme-toggle').click()");
                        _ = web.CoreWebView2.ExecuteScriptAsync("setTimeout(() => window.chrome.webview.postMessage({type:'designer',event:'command',payload:{command:'edit-custom-properties'}}), 200)");
                    }));
                }
                var editor = (ComponentEditorView?)Field(builder, "_componentEditor");
                if (editor is null) return;
                var status = ((Label)Field(editor, "_status")!).Text;
                if (status != phase) { phase = status; Console.WriteLine($"Editor: {status}"); }
                if (Field(editor, "_editorReady") is not true) return;
                if (args.Contains("--light") && !Equals(Field(editor, "_theme"), "light"))
                    throw new InvalidOperationException("Workspace theme did not reach the code editor.");
                if (editor.Parent != builder || editor.Width < 100 || editor.Height < 100)
                    throw new InvalidOperationException("Code editor is not docked in the Builder workspace.");
                if (((Label)Field(editor, "_status")!).Visible)
                    throw new InvalidOperationException("Duplicate native status bar is visible after editor initialization.");
                // The ready bridge message is sent only after CodeMirror creates
                // all three views. DOM details are covered by code-editor.test.cjs.
                var source = (ComponentCustomization)Field(editor, "_draft")!;
                if (!source.Css.Contains(":host"))
                    throw new InvalidOperationException("Initial component sources are missing.");
                passed = true;
                Console.WriteLine("PASS: docked editor opened from a WebView2 command, initialized CodeMirror, and received component sources without a duplicate status bar.");
                timer.Stop();
                builder.BeginInvoke(new Action(() => {
                    _ = FinishAsync(builder, editor, args.FirstOrDefault());
                }));
            } catch (Exception error) {
                Console.Error.WriteLine(error); timer.Stop();
                ((BuilderViewModel)Field(builder, "_viewModel")!).MarkSaved();
                builder.BeginInvoke(new Action(() => {
                    foreach (var window in Application.OpenForms.Cast<Form>().Where(window => window != builder).ToArray()) window.Close();
                    builder.BeginInvoke(new Action(builder.Close));
                }));
            } finally { checking = false; }
        };
        timer.Start(); Application.Run(builder);
        Environment.ExitCode = passed ? 0 : 1;
    }

    private static async Task FinishAsync(BuilderWindow builder, ComponentEditorView editor, string? capturePath)
    {
        try {
            if (capturePath is not null) {
                using var background = new MemoryStream(); using var foreground = new MemoryStream();
                await ((WebView2)Field(builder, "_preview")!).CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, background);
                await ((WebView2)Field(editor, "_web")!).CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, foreground);
                background.Position = 0; foreground.Position = 0;
                using var bitmap = new Bitmap(background); using var code = new Bitmap(foreground);
                using (var graphics = Graphics.FromImage(bitmap)) graphics.DrawImage(code, editor.Bounds);
                bitmap.Save(capturePath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"Workspace capture: {capturePath}");
            }
        } catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
        finally {
            editor.Close();
            ((BuilderViewModel)Field(builder, "_viewModel")!).MarkSaved();
            builder.BeginInvoke(new Action(builder.Close));
        }
    }
}
