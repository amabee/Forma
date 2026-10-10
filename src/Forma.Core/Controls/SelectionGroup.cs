namespace Forma.Core.Controls;

public abstract class SelectionGroup : ChoiceControl
{
    private string _orientation = "horizontal";
    public string Orientation { get => _orientation; set { if (value is "horizontal" or "vertical") SetProperty(ref _orientation, value); } }
}
