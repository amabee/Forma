namespace Forma.Builder;

public sealed class Appearance
{
    public Appearance() { }

    public int Width { get; set; } = 120;
    public int Height { get; set; } = 36;
    private string _dock = "none";
    private string _anchor = "top,left";
    public static readonly string[] DockValues = ["none", "top", "bottom", "left", "right", "fill"];
    public static readonly string[] AnchorValues = ["none", "top,left", "top,right", "bottom,left", "bottom,right", "top,left,right", "bottom,left,right", "top,bottom,left", "top,bottom,right", "top,bottom,left,right", "top", "bottom", "left", "right", "top,bottom", "left,right"];
    public string Dock
    {
        get => _dock;
        set => _dock = DockValues.Contains(value) ? value : throw new ArgumentException("Invalid Dock value.");
    }
    public string Anchor
    {
        get => _anchor;
        set => _anchor = AnchorValues.Contains(value) ? value : throw new ArgumentException("Invalid Anchor value.");
    }
    public string ForeColor { get; set; } = "#ffffff";
    public string BackColor { get; set; } = "#2878ff";
    public int FontSize { get; set; } = 14;
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = true;
    public string FontFamily { get; set; } = "Segoe UI";
    public string FontWeight { get; set; } = "normal";
    public string FontStyle { get; set; } = "normal";
    public string TextAlign { get; set; } = "center";
    public string BorderStyle { get; set; } = "solid";
    public string BorderColor { get; set; } = "#d3deee";
    public int BorderWidth { get; set; } = 1;
    public int BorderRadius { get; set; } = 4;
    public int Padding { get; set; } = 8;
    public int Opacity { get; set; } = 100;
    public string Placeholder { get; set; } = "Enter your placeholder";
    public bool ReadOnly { get; set; }
    public bool Password { get; set; }
    public int MaxLength { get; set; } = 32767;
    public string Tag { get; set; } = "";
    public bool Locked { get; set; }
    public string Style { get; set; } = "Custom";
    public string Shadow { get; set; } = "None";
    public string Cursor { get; set; } = "default";
    public double LineHeight { get; set; } = 1.5;
    public double LetterSpacing { get; set; }
    public bool Focusable { get; set; } = true;
    public int TabIndex { get; set; }
    public string ToolTip { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string CustomCss { get; set; } = "";
    public ComponentCustomization? Customization { get; set; }
    public string GlobalScript { get; set; } = "";
    public int ZIndex { get; set; }
    public int MarginTop { get; set; }
    public int MarginRight { get; set; }
    public int MarginBottom { get; set; }
    public int MarginLeft { get; set; }
    public int PaddingTop { get; set; } = 8;
    public int PaddingRight { get; set; } = 8;
    public int PaddingBottom { get; set; } = 8;
    public int PaddingLeft { get; set; } = 8;
    public int MinimumWidth { get; set; }
    public int MinimumHeight { get; set; }
    public int MaximumWidth { get; set; }
    public int MaximumHeight { get; set; }
}
