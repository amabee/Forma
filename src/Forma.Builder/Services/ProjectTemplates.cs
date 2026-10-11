using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>
/// Starter designs for the start page. Each is built through ordinary designer edits, so a
/// template is always a valid project and stays in step with control defaults.
/// </summary>
public static class ProjectTemplates
{
    public sealed record Template(string Id, string Name, string Description);

    public static readonly Template[] All =
    [
        new("login", "Login form", "Header, username and password fields, and a sign-in button."),
        new("settings", "Settings dialog", "Grouped options with toggles, a dropdown and Save/Cancel."),
        new("dashboard", "Dashboard", "Header bar, side navigation and summary cards."),
    ];

    /// <summary>Replaces <paramref name="model"/>'s design with the template. Returns false for unknown IDs.</summary>
    public static bool Apply(BuilderViewModel model, string id)
    {
        if (All.All(template => template.Id != id)) return false;
        model.CreateNew();
        var form = model.Form!;
        FControl Add(string kind, int x, int y, FControl? parent = null) =>
            model.ExecuteEdit("drop", (parent ?? form).Id, Payload(new { control = kind, x, y })).AddedControl
                ?? throw new InvalidOperationException($"Template could not add {kind}.");
        void Set(FControl control, string property, object value)
        {
            model.ExecuteEdit("select", control.Id, default);
            model.ExecuteEdit("property", control.Id, Payload(new { property, value }));
        }
        void Size(FControl control, int width, int height) { Set(control, "width", width); Set(control, "height", height); }

        switch (id)
        {
            case "login":
                Set(form, "text", "Sign in");
                Size(form, 420, 380);
                var title = Add("label", 40, 32); Set(title, "text", "Welcome back"); Size(title, 340, 44);
                var user = Add("textbox", 40, 110); Size(user, 340, 36); Set(user, "placeholder", "Username");
                var password = Add("passwordbox", 40, 160); Size(password, 340, 36);
                var remember = Add("checkbox", 40, 210); Set(remember, "text", "Remember me");
                var signIn = Add("button", 40, 270); Set(signIn, "text", "Sign in"); Size(signIn, 340, 40);
                break;
            case "settings":
                Set(form, "text", "Settings");
                Size(form, 480, 420);
                var group = Add("groupbox", 24, 24); Set(group, "text", "Preferences"); Size(group, 432, 260);
                var notifications = Add("toggleswitch", 16, 16, group); Set(notifications, "text", "Notifications");
                var updates = Add("toggleswitch", 16, 64, group); Set(updates, "text", "Automatic updates");
                var theme = Add("combobox", 16, 120, group); Set(theme, "items", "Light\nDark\nSystem"); Size(theme, 220, 36);
                var cancel = Add("button", 248, 330); Set(cancel, "text", "Cancel"); Size(cancel, 96, 40);
                var save = Add("button", 360, 330); Set(save, "text", "Save"); Size(save, 96, 40);
                break;
            case "dashboard":
                Set(form, "text", "Dashboard");
                Size(form, 900, 560);
                var header = Add("panel", 0, 0); Set(header, "dock", "top"); Set(header, "height", 56);
                Set(header, "borderSides", "bottom");
                var heading = Add("label", 16, 8, header); Set(heading, "text", "Overview"); Size(heading, 240, 40);
                var navigation = Add("sidenavigation", 0, 0); Set(navigation, "dock", "left");
                for (var index = 0; index < 3; index++)
                {
                    // Size before positioning: a move is clamped against the control's current width.
                    var card = Add("card", 220, 80); Size(card, 200, 140); Set(card, "x", 220 + index * 220);
                    Set(card, "text", new[] { "Revenue", "Users", "Orders" }[index]);
                }
                break;
        }
        model.ExecuteEdit("select", form.Id, default);
        model.ClearHistory();
        return true;
    }

    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
}
