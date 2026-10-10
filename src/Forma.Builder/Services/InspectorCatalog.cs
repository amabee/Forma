namespace Forma.Builder;

/// <summary>Editor metadata supplied by C#; the browser builds contextual editors from it.</summary>
public sealed record InspectorProperty(
    string Id,
    string Label,
    string Category,
    string Editor = "text",
    double? Min = null,
    double? Max = null,
    string[]? Options = null,
    string? Kind = null,
    bool ReadOnly = false,
    bool ChildOnly = false
);

public static class InspectorCatalog
{
    public static readonly InspectorProperty[] Properties =
    [
        new("name", "Name", "General"),
        new("id", "ID", "General", ReadOnly: true),
        new("text", "Text / Title", "General"),
        new("tag", "Tag", "General"),
        new("locked", "Locked", "General", "checkbox"),
        new(
            "description",
            "Description",
            "General",
            "textarea",
            Kind: "card,emptystate,commandbutton"
        ),
        new(
            "iconName",
            "Icon",
            "Appearance",
            "select",
            Options: Forma.Core.Controls.Icon.Names.ToArray(),
            Kind: "icon,emptystate,iconbutton,floatingactionbutton,commandbutton"
        ),
        new("strokeWidth", "Stroke width", "Appearance", "number", 1, 4, Kind: "icon"),
        new(
            "shape",
            "Shape",
            "Appearance",
            "select",
            Options: ["text", "rectangle", "circle"],
            Kind: "skeleton"
        ),
        new("lines", "Lines", "Appearance", "number", 1, 10, Kind: "skeleton"),
        new("headerVisible", "Show header", "Appearance", "checkbox", Kind: "card"),
        new(
            "variant",
            "Variant",
            "Appearance",
            "select",
            Options: ["neutral", "info", "success", "warning", "danger"],
            Kind: "badge,chip"
        ),
        new(
            "variant",
            "Variant",
            "Appearance",
            "select",
            Options: Forma.Core.Controls.Toast.Variants.ToArray(),
            Kind: "toast"
        ),
        new("initials", "Initials (blank=automatic)", "General", Kind: "avatar"),
        new(
            "shape",
            "Shape",
            "Appearance",
            "select",
            Options: ["circle", "rounded", "square"],
            Kind: "avatar"
        ),
        new("thickness", "Line thickness", "Appearance", "number", 1, 12, Kind: "divider"),
        new(
            "lineStyle",
            "Line style",
            "Appearance",
            "select",
            Options: ["solid", "dashed", "dotted"],
            Kind: "divider"
        ),
        new(
            "position",
            "Position",
            "Behavior",
            "select",
            Options: ["top-right", "top-left", "bottom-right", "bottom-left"],
            Kind: "toast"
        ),
        new("duration", "Duration (ms)", "Behavior", "number", 500, 60000, Kind: "toast"),
        new("dismissible", "Allow dismissal", "Behavior", "checkbox", Kind: "toast"),
        new("isActive", "Active", "Behavior", "checkbox", Kind: "spinner,loadingoverlay,skeleton"),
        new("speed", "Animation speed (ms)", "Behavior", "number", 100, 5000, Kind: "spinner"),
        new(
            "style",
            "Style",
            "Appearance",
            "select",
            Options: ["Default", "Primary", "Secondary", "Success", "Warning", "Danger", "Custom"],
            Kind: "button"
        ),
        new("backColor", "Background", "Appearance", "color"),
        new("foreColor", "Foreground", "Appearance", "color"),
        new(
            "borderStyle",
            "Border style",
            "Appearance",
            "select",
            Options: ["none", "solid", "dashed", "dotted"],
            ChildOnly: true
        ),
        new("borderColor", "Border color", "Appearance", "color", ChildOnly: true),
        new("borderWidth", "Border width", "Appearance", "number", 0, 10, ChildOnly: true),
        new("borderRadius", "Radius", "Appearance", "number", 0, 100, ChildOnly: true),
        new("opacity", "Opacity (%)", "Appearance", "number", 10, 100, ChildOnly: true),
        new(
            "shadow",
            "Shadow",
            "Appearance",
            "select",
            Options: ["None", "Small", "Medium", "Large"],
            ChildOnly: true
        ),
        new(
            "cursor",
            "Cursor",
            "Appearance",
            "select",
            Options: ["default", "pointer", "text", "crosshair", "help", "not-allowed"],
            ChildOnly: true
        ),
        new(
            "fontFamily",
            "Font family",
            "Typography",
            "select",
            Options: ["Segoe UI", "Arial", "Consolas", "Georgia"]
        ),
        new("fontSize", "Font size", "Typography", "number", 8, 48),
        new(
            "fontWeight",
            "Weight",
            "Typography",
            "select",
            Options:
            [
                "normal",
                "bold",
                "100",
                "200",
                "300",
                "400",
                "500",
                "600",
                "700",
                "800",
                "900",
            ]
        ),
        new("fontStyle", "Font style", "Typography", "select", Options: ["normal", "italic"]),
        new("textAlign", "Alignment", "Typography", "select", Options: ["left", "center", "right"]),
        new("lineHeight", "Line height", "Typography", "number", .5, 4),
        new("letterSpacing", "Letter spacing", "Typography", "number", -5, 20),
        new("x", "X", "Layout", "number", 0, 1600, ChildOnly: true),
        new("y", "Y", "Layout", "number", 0, 1200, ChildOnly: true),
        new("width", "Width", "Layout", "number", 24, 1600),
        new("height", "Height", "Layout", "number", 20, 1200),
        new("minimumWidth", "Min width", "Layout", "number", 0, 1600),
        new("minimumHeight", "Min height", "Layout", "number", 0, 1200),
        new("maximumWidth", "Max width (0=auto)", "Layout", "number", 0, 1600),
        new("maximumHeight", "Max height (0=auto)", "Layout", "number", 0, 1200),
        new("marginTop", "Margin top", "Layout", "number", 0, 64, ChildOnly: true),
        new("marginRight", "Margin right", "Layout", "number", 0, 64, ChildOnly: true),
        new("marginBottom", "Margin bottom", "Layout", "number", 0, 64, ChildOnly: true),
        new("marginLeft", "Margin left", "Layout", "number", 0, 64, ChildOnly: true),
        new("paddingTop", "Padding top", "Layout", "number", 0, 64, ChildOnly: true),
        new("paddingRight", "Padding right", "Layout", "number", 0, 64, ChildOnly: true),
        new("paddingBottom", "Padding bottom", "Layout", "number", 0, 64, ChildOnly: true),
        new("paddingLeft", "Padding left", "Layout", "number", 0, 64, ChildOnly: true),
        new("enabled", "Enabled", "Behavior", "checkbox", ChildOnly: true),
        new("visible", "Visible", "Behavior", "checkbox", ChildOnly: true),
        new("focusable", "Focusable", "Behavior", "checkbox", ChildOnly: true),
        new("tabIndex", "Tab index", "Behavior", "number", 0, 32767, ChildOnly: true),
        new("toolTip", "Tooltip", "Behavior", ChildOnly: true),
        new(
            "placeholder",
            "Placeholder",
            "Behavior",
            Kind: "textbox,maskedtextbox,searchbox,passwordbox,textarea"
        ),
        new(
            "readOnly",
            "Read only",
            "Behavior",
            "checkbox",
            Kind: "textbox,maskedtextbox,searchbox,passwordbox,textarea"
        ),
        new(
            "password",
            "Password",
            "Behavior",
            "checkbox",
            Kind: "textbox,maskedtextbox,searchbox,passwordbox,textarea"
        ),
        new(
            "maxLength",
            "Max length",
            "Behavior",
            "number",
            1,
            32767,
            Kind: "textbox,maskedtextbox,searchbox,passwordbox,textarea"
        ),
        new("cssClass", "CSS class", "Advanced"),
        new("id", "CSS ID", "Advanced", ReadOnly: true),
        new(
            "minimum",
            "Minimum",
            "Behavior",
            "number",
            Kind: "numericupdown,slider,progressbar,circularprogress"
        ),
        new(
            "maximum",
            "Maximum",
            "Behavior",
            "number",
            Kind: "numericupdown,slider,progressbar,circularprogress"
        ),
        new("increment", "Step", "Behavior", "number", .001, 1000000, Kind: "numericupdown,slider"),
        new(
            "number",
            "Value",
            "Behavior",
            "number",
            Kind: "numericupdown,slider,progressbar,circularprogress"
        ),
        new("dateValue", "Value", "Behavior", "date", Kind: "datepicker"),
        new("dateValue", "Value", "Behavior", "time", Kind: "timepicker"),
        new("dateValue", "Value", "Behavior", "datetime-local", Kind: "datetimepicker"),
        new("color", "Selected color", "Behavior", "color", Kind: "colorpicker"),
        new("nodes", "Nodes (JSON)", "General", "textarea", Kind: "treeview"),
        new(
            "commandItems",
            "Commands (JSON)",
            "General",
            "textarea",
            Kind: "menustrip,toolbar,toolstrip,contextmenu,contextmenustrip,dropdownbutton,splitbutton"
        ),
        new(
            "targetId",
            "Target",
            "Behavior",
            "target",
            Kind: "contextmenu,contextmenustrip,tooltip,loadingoverlay"
        ),
        new("initialDelay", "Initial delay (ms)", "Behavior", "number", 0, 10000, Kind: "tooltip"),
        new("showDuration", "Duration (ms)", "Behavior", "number", 500, 60000, Kind: "tooltip"),
        new(
            "placement",
            "Placement",
            "Behavior",
            "select",
            Options: ["top", "bottom", "left", "right"],
            Kind: "tooltip"
        ),
        new("dialogTitle", "Dialog title", "General", Kind: "dialog,confirmationdialog"),
        new("message", "Message", "General", "textarea", Kind: "dialog,confirmationdialog"),
        new(
            "buttons",
            "Buttons",
            "Behavior",
            "select",
            Options: ["OK", "OKCancel", "YesNo", "YesNoCancel"],
            Kind: "dialog,confirmationdialog"
        ),
        new("canCancel", "Allow Escape", "Behavior", "checkbox", Kind: "dialog,confirmationdialog"),
        new("result", "Last result", "Behavior", Kind: "dialog,confirmationdialog", ReadOnly: true),
        new("rightText", "Right text", "General", Kind: "statusbar"),
        new(
            "selectedPath",
            "Selected path",
            "General",
            Kind: "filepicker,folderpicker",
            ReadOnly: true
        ),
        new("dialogTitle", "Dialog title", "Behavior", Kind: "filepicker,folderpicker"),
        new("filter", "File filter", "Behavior", Kind: "filepicker"),
        new("entries", "Properties (JSON)", "General", "textarea", Kind: "propertygrid"),
        new("readOnly", "Read only", "Behavior", "checkbox", Kind: "propertygrid"),
        new("selectedNode", "Selected node ID", "Behavior", Kind: "treeview"),
        new("expandedNodes", "Expanded node IDs", "Behavior", Kind: "treeview"),
        new("totalItems", "Total items", "Behavior", "number", 0, 2147483647, Kind: "pagination"),
        new("pageSize", "Page size", "Behavior", "number", 1, 10000, Kind: "pagination"),
        new("page", "Page", "Behavior", "number", 1, 2147483647, Kind: "pagination"),
        new("document", "Rich document (JSON)", "Advanced", "textarea", Kind: "richtextbox"),
        new("readOnly", "Read only", "Behavior", "checkbox", Kind: "richtextbox"),
        new("url", "URL", "Behavior", Kind: "linklabel"),
        new("mask", "Mask", "Behavior", Kind: "maskedtextbox"),
        new(
            "maskCompleted",
            "Complete",
            "Behavior",
            "checkbox",
            Kind: "maskedtextbox",
            ReadOnly: true
        ),
        new(
            "checkedIndices",
            "Checked indices",
            "Behavior",
            Kind: "checkedlistbox,checkboxgroup,chipgroup"
        ),
        new(
            "showText",
            "Show text",
            "Appearance",
            "checkbox",
            Kind: "iconbutton,floatingactionbutton,commandbutton"
        ),
        new("primaryEnabled", "Primary enabled", "Behavior", "checkbox", Kind: "splitbutton"),
        new("removable", "Removable", "Behavior", "checkbox", Kind: "chip"),
        new("stars", "Stars", "Behavior", "number", 1, 10, Kind: "rating"),
        new("number", "Value", "Behavior", "number", 0, 10, Kind: "rating"),
        new("readOnly", "Read only", "Behavior", "checkbox", Kind: "rating"),
        new(
            "checked",
            "Checked",
            "Behavior",
            "checkbox",
            Kind: "checkbox,radiobutton,toggleswitch,togglebutton,chip"
        ),
        new(
            "items",
            "Items (one per line)",
            "General",
            "textarea",
            Kind: "combobox,listbox,listview,checkedlistbox,radiogroup,checkboxgroup,segmentedcontrol,chipgroup,buttongroup,breadcrumb,sidenavigation"
        ),
        new(
            "selectedIndex",
            "Selected index",
            "Behavior",
            "number",
            -1,
            10000,
            Kind: "combobox,listbox,listview,radiogroup,segmentedcontrol,buttongroup,breadcrumb,sidenavigation"
        ),
        new("source", "Image source", "General", Kind: "image,picturebox,avatar", ReadOnly: true),
        new(
            "sizeMode",
            "Size mode",
            "Appearance",
            "select",
            Options: ["contain", "cover", "fill"],
            Kind: "image,picturebox,avatar"
        ),
        new(
            "orientation",
            "Orientation",
            "Layout",
            "select",
            Options: ["horizontal", "vertical"],
            Kind: "splitcontainer,flowlayoutpanel,tabcontrol,toolbar,toolstrip,divider,radiogroup,checkboxgroup,segmentedcontrol,chipgroup,buttongroup,sidebar,appshell,responsivepanel,stackpanel,hstack,vstack,wrappanel,centerpanel,sidenavigation"
        ),
        new(
            "gap",
            "Gap",
            "Layout",
            "number",
            0,
            64,
            Kind: "splitcontainer,flowlayoutpanel,tablelayoutpanel,sidebar,appshell,responsivepanel,accordion,stackpanel,hstack,vstack,wrappanel,centerpanel"
        ),
        new(
            "scrollDirection",
            "Scroll direction",
            "Layout",
            "select",
            Options: ["both", "horizontal", "vertical"],
            Kind: "scrollablepanel"
        ),
        new("columns", "Columns", "Layout", "number", 1, 12, Kind: "tablelayoutpanel"),
        new("rowCount", "Rows", "Layout", "number", 1, 100, Kind: "tablelayoutpanel"),
        new(
            "breakpoint",
            "Stack below width",
            "Layout",
            "number",
            100,
            2400,
            Kind: "appshell,responsivepanel"
        ),
        new("tabs", "Sections (one per line)", "General", "textarea", Kind: "accordion"),
        new("expanded", "Expanded", "Behavior", "checkbox", Kind: "accordion"),
        new("selectedTab", "Expanded section", "Behavior", "number", 0, 99, Kind: "accordion"),
        new("tabs", "Tabs (one per line)", "General", "textarea", Kind: "tabcontrol"),
        new("selectedTab", "Selected tab", "Behavior", "number", 0, 99, Kind: "tabcontrol"),
        new("layoutSlot", "Pane / tab / cell", "Layout", "number", 1, 100, ChildOnly: true),
        new("dock", "Dock", "Layout", "select", Options: Appearance.DockValues, ChildOnly: true),
        new(
            "anchor",
            "Anchor",
            "Layout",
            "select",
            Options: Appearance.AnchorValues,
            ChildOnly: true
        ),
        new("gridColumns", "Columns (one per line)", "General", "textarea", Kind: "datagridview"),
        new("gridRows", "Rows (JSON)", "General", "textarea", Kind: "datagridview"),
        new("readOnly", "Read only", "Behavior", "checkbox", Kind: "datagridview"),
        new("sortingEnabled", "Allow sorting", "Behavior", "checkbox", Kind: "datagridview"),
        new("filteringEnabled", "Allow filtering", "Behavior", "checkbox", Kind: "datagridview"),
        new("filterText", "Filter text", "Behavior", Kind: "datagridview"),
        new(
            "sortColumn",
            "Sort column (-1=none)",
            "Behavior",
            "number",
            -1,
            99,
            Kind: "datagridview"
        ),
        new(
            "sortDirection",
            "Sort direction",
            "Behavior",
            "select",
            Options: ["ascending", "descending"],
            Kind: "datagridview"
        ),
        new(
            "selectedRow",
            "Selected row (-1=none)",
            "Behavior",
            "number",
            -1,
            100000,
            Kind: "datagridview"
        ),
        new("interval", "Interval (ms)", "Behavior", "number", 10, 3600000, Kind: "timer"),
        new(
            "workerReportsProgress",
            "Report progress",
            "Behavior",
            "checkbox",
            Kind: "backgroundworker"
        ),
        new(
            "workerSupportsCancellation",
            "Cancellation",
            "Behavior",
            "checkbox",
            Kind: "backgroundworker"
        ),
        new("isBusy", "Busy", "Behavior", "checkbox", Kind: "backgroundworker", ReadOnly: true),
    ];

    public static IEnumerable<InspectorProperty> ForKind(string kind) =>
        Properties.Where(p =>
            (!p.ChildOnly || kind != "form")
            && (p.Kind is null || p.Kind.Split(',').Contains(kind))
            && (
                !(
                    kind
                    is "timer"
                        or "backgroundworker"
                        or "contextmenu"
                        or "contextmenustrip"
                        or "dialog"
                        or "confirmationdialog"
                        or "tooltip"
                        or "toast"
                        or "loadingoverlay"
                )
                || p.Category == "General"
                || p.Id
                    is "enabled"
                        or "interval"
                        or "workerReportsProgress"
                        or "workerSupportsCancellation"
                        or "isBusy"
                        or "isActive"
                        or "targetId"
                        or "buttons"
                        or "canCancel"
                        or "result"
                        or "initialDelay"
                        or "showDuration"
                        or "placement"
                        or "variant"
                        or "position"
                        or "duration"
                        or "dismissible"
            )
        );
}
