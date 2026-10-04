namespace Forma.Core.Controls;

public sealed class Card : LayoutContainer
{
    private string _description = "Add content to this card.";
    private bool _headerVisible = true;
    public Card() => Text = "Card title";
    public string Description { get => _description; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _description, value); } }
    public bool HeaderVisible { get => _headerVisible; set => SetProperty(ref _headerVisible, value); }
}
