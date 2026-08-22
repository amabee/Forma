using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Forma.Core.Controls;

// props props props

/// <summary>
/// Represents a base class for all controls in the Forma framework.
/// </summary>
public abstract class Control : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private string? _text;
    private readonly List<Control> _children = [];

    /// <summary>
    /// Stable identity used by renderers to address this control.
    /// </summary>
    /// <remarks>
    /// Set-once: a renderer keys its live control registry by this value, so a
    /// mutable id would silently orphan the registration and the DOM node.
    /// </remarks>
    public string Id
    {
        get => _id;
        init => _id = value;
    }

    public string? Name { get; set; }
    public string? Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
            else
            {
                return;
            }
        }
    }
    /// <summary>
    /// The kind of control, as understood by a renderer.
    /// </summary>
    /// <remarks>
    /// Defaults to the lowercased type name, so <c>Button</c> renders as
    /// <c>button</c> and a new control type needs no renderer change. Override
    /// when the type name and the rendered kind should differ.
    /// </remarks>
    public virtual string ControlType => GetType().Name.ToLowerInvariant();

    public Control? Parent { get; private set; }

    /// <summary>
    /// Child controls, in render order. Mutate through <see cref="Add"/> and
    /// <see cref="Remove"/> so parent links and renderer notifications stay
    /// consistent.
    /// </summary>
    public IReadOnlyList<Control> Children => _children;

    /// <summary>Raised after a child is added to this control.</summary>
    public event EventHandler<ControlEventArgs>? ChildAdded;

    /// <summary>Raised after a child is removed from this control.</summary>
    public event EventHandler<ControlEventArgs>? ChildRemoved;

    /// <summary>Appends <paramref name="child"/> to this control.</summary>
    /// <exception cref="InvalidOperationException">
    /// The child already belongs to a parent, or the add would create a cycle.
    /// </exception>
    public void Add(Control child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (ReferenceEquals(child, this))
        {
            throw new InvalidOperationException("A control cannot contain itself.");
        }

        if (child.Parent is not null)
        {
            throw new InvalidOperationException(
                $"Control '{child.Id}' already belongs to '{child.Parent.Id}'. "
                    + "Remove it from its current parent first."
            );
        }

        // Walking ancestors keeps the tree acyclic, which every renderer and
        // traversal below depends on.
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, child))
            {
                throw new InvalidOperationException(
                    $"Adding '{child.Id}' to '{Id}' would create a cycle."
                );
            }
        }

        _children.Add(child);

        child.Parent = this;

        ChildAdded?.Invoke(this, new ControlEventArgs(child));
    }

    /// <summary>Removes <paramref name="child"/> from this control.</summary>
    /// <returns><c>true</c> if the child was present.</returns>
    public bool Remove(Control child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (!_children.Remove(child))
        {
            return false;
        }

        child.Parent = null;

        ChildRemoved?.Invoke(this, new ControlEventArgs(child));

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null
    )
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
