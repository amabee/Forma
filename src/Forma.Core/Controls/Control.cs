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

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
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
    public Control? Parent { get; internal set; }
    public List<Control> Children { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null
    )
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
