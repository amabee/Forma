namespace Forma.Core.Controls;

/// <summary>A check box that is mutually exclusive with the radio buttons sharing its parent.</summary>
public sealed class RadioButton : CheckBox
{
    protected override void OnCheckedChanged()
    {
        // Uncheck siblings before notifying so observers never see two checked radios.
        if (Checked && Parent is not null)
            foreach (var sibling in Parent.Children.OfType<RadioButton>())
                if (sibling != this) sibling.Checked = false;
        base.OnCheckedChanged();
    }
}
