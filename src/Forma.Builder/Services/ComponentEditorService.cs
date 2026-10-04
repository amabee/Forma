using System.Globalization;
using System.Text.Json;
using Control = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Creates portable editing files; applied sources are embedded in the project.</summary>
public static class ComponentEditorService
{
    public const string PropertiesFile = "custom-properties.json";
    public static ComponentCustomization Template(Control control, Appearance appearance)
    {
        if (appearance.Customization is not null) return ComponentCustomization.Validate(appearance.Customization);
        string Number(double value) => value.ToString(CultureInfo.InvariantCulture);
        return new()
        {
            Css = $$"""
                /* {{control.Name ?? control.ControlType}} — styles scoped to this component.
                   :host means this component. Use :host:hover or :host input, etc.
                   These are your current appearance defaults; edit them as needed.
                   Keep position and sizing in the Layout properties. */
                :host {
                  color: {{appearance.ForeColor}};
                  background-color: {{appearance.BackColor}};
                  font-family: {{JsonSerializer.Serialize(appearance.FontFamily)}};
                  font-size: {{appearance.FontSize}}px;
                  font-weight: {{appearance.FontWeight}};
                  font-style: {{appearance.FontStyle}};
                  text-align: {{appearance.TextAlign}};
                  line-height: {{Number(appearance.LineHeight)}};
                  letter-spacing: {{Number(appearance.LetterSpacing)}}px;
                  border: {{appearance.BorderWidth}}px {{appearance.BorderStyle}} {{appearance.BorderColor}};
                  border-radius: {{appearance.BorderRadius}}px;
                  padding: {{appearance.PaddingTop}}px {{appearance.PaddingRight}}px {{appearance.PaddingBottom}}px {{appearance.PaddingLeft}}px;
                  opacity: {{Number(appearance.Opacity / 100d)}};
                  cursor: {{appearance.Cursor}};
                  z-index: {{appearance.ZIndex}};
                  {{appearance.CustomCss}}
                }

                :host:hover {
                  /* Add a hover style here. */
                }
                """,
            Behavior = """
                // JavaScript runs once when Preview starts, never in the designer.
                // component.element: this control's DOM element
                // component.properties: your custom JSON values (runtime copy)
                // api.on(event, handler): listen on this component, cleaned up on exit
                // api.get(nameOrId, property), api.set(nameOrId, property, value)
                // Properties: text, enabled, visible, checked, value, selectedIndex, selectedTab.
                // api.showDialog(nameOrId): open a Dialog/ConfirmationDialog.
                // api.showToast(nameOrId), api.closeToast(nameOrId): notification lifecycle.
                // Spinner/LoadingOverlay: api.set(nameOrId, "isActive", true/false).
                // api.find(nameOrId): another control's DOM element.
                // api.cleanup(callback): register cleanup for your own resources.
                // Timer components can use api.on("tick", ...).
                // DataGrid: api.on("row-selection", e => { ... e.detail.row ... });
                // DataGrid properties: selectedRow, filterText, sortColumn, sortDirection.

                api.on("click", () => {
                  // Example (uncomment and replace label1 with your label's Name):
                  // component.properties.clicks += 1;
                  // api.set("label1", "text", `Clicked ${component.properties.clicks} times`);
                });
                """,
            Characteristics = """
                {
                  "clicks": 0,
                  "description": "My custom component"
                }
                """
        };
    }

    public static string Folder(BuilderViewModel design, Control control)
    {
        var root = design.ProjectPath is string project
            ? Path.Combine(Path.GetDirectoryName(project)!, Path.GetFileNameWithoutExtension(project) + ".components")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Forma", "ComponentEditors", design.Form!.Id);
        return Path.Combine(root, control.Id);
    }

    public static void Write(string folder, ComponentCustomization source)
    {
        source = ComponentCustomization.Validate(source);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "component.css"), source.Css);
        File.WriteAllText(Path.Combine(folder, "behavior.js"), source.Behavior);
        File.WriteAllText(Path.Combine(folder, PropertiesFile), source.Characteristics);
    }

    public static ComponentCustomization Read(string folder) => ComponentCustomization.Validate(new()
    {
        Css = ReadFile(Path.Combine(folder, "component.css")),
        Behavior = ReadFile(Path.Combine(folder, "behavior.js")),
        Characteristics = ReadFile(File.Exists(Path.Combine(folder, PropertiesFile))
            ? Path.Combine(folder, PropertiesFile) : Path.Combine(folder, "characteristics.json"))
    });

    private static string ReadFile(string path)
    {
        if (new FileInfo(path).Length > 800_000) throw new ArgumentException("The component file is too large.");
        return File.ReadAllText(path);
    }
}
