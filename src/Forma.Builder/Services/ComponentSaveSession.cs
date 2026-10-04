namespace Forma.Builder;

/// <summary>Commits valid saves once, including the first save of untouched boilerplate.</summary>
public sealed class ComponentSaveSession(ComponentCustomization initial, Func<ComponentCustomization, Task> apply)
{
    private bool _hasApplied;
    public ComponentCustomization Current { get; private set; } = initial;

    public bool IsCurrent(ComponentCustomization source) => _hasApplied
        && source.Css == Current.Css && source.Behavior == Current.Behavior
        && source.Characteristics == Current.Characteristics;

    public async Task<bool> ApplyAsync(ComponentCustomization source)
    {
        source = ComponentCustomization.Validate(source);
        if (IsCurrent(source)) return false;
        await apply(source);
        Current = source;
        _hasApplied = true;
        return true;
    }
}
