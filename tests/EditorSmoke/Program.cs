using System.Reflection;
using System.Text.Json;
using Forma.Builder;
using Microsoft.Web.WebView2.WinForms;

// Integration regression: open the modal editor through a real WebView2
// message, not a direct method call, and verify its browser finishes loading.
internal static class Program
{
    private static object? Field(object instance, string name) => instance.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var builder = new BuilderWindow {
            StartPosition = FormStartPosition.Manual, Location = new(-30000, -30000),
            ShowInTaskbar = false
        };
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var sent = false; var checking = false; var passed = false;
        timer.Tick += (_, _) => {
            if (checking) return;
            checking = true;
            try {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Modal code editor did not become ready.");
                if (!sent && Field(builder, "_ready") is true) {
                    Console.WriteLine("Designer ready; requesting modal editor.");
                    var model = (BuilderViewModel)Field(builder, "_viewModel")!;
                    model.ExecuteEdit("drop", model.Form!.Id, JsonSerializer.SerializeToElement(new { control = "button", x = 10, y = 10 }));
                    sent = true;
                    var web = (WebView2)Field(builder, "_preview")!;
                    // Return from the timer before injecting the command so its
                    // watchdog keeps ticking inside the modal message loop.
                    builder.BeginInvoke(new Action(() => {
                        _ = web.CoreWebView2.ExecuteScriptAsync("setTimeout(() => window.chrome.webview.postMessage({type:'designer',event:'command',payload:{command:'edit-custom-properties'}}), 200)");
                    }));
                }
                var editor = Application.OpenForms.OfType<ComponentEditorWindow>().FirstOrDefault();
                if (editor is null) return;
                editor.ShowInTaskbar = false;
                if (Field(editor, "_editorReady") is not true) return;
                if (((Label)Field(editor, "_status")!).Visible)
                    throw new InvalidOperationException("Duplicate native status bar is visible after editor initialization.");
                // The ready bridge message is sent only after CodeMirror creates
                // all three views. DOM details are covered by code-editor.test.cjs.
                var source = (ComponentCustomization)Field(editor, "_draft")!;
                if (!source.Css.Contains(":host"))
                    throw new InvalidOperationException("Initial component sources are missing.");
                passed = true;
                Console.WriteLine("PASS: modal editor opened from a WebView2 callback, initialized CodeMirror, and received component sources.");
                timer.Stop();
                builder.BeginInvoke(new Action(() => {
                    editor.Close();
                    ((BuilderViewModel)Field(builder, "_viewModel")!).MarkSaved();
                    builder.BeginInvoke(new Action(builder.Close));
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
}
