using Forma.Core;
using Forma.Core.Controls;

namespace Forma.Tests;

public class ControlTreeTests
{
    [Fact]
    public void Add_LinksChildToParent()
    {
        var form = new Form { Id = "form1" };
        var button = new Button { Id = "btn1" };

        form.Add(button);

        Assert.Same(form, button.Parent);
        Assert.Equal([button], form.Children);
    }

    [Fact]
    public void Add_PreservesInsertionOrder()
    {
        var form = new Form { Id = "form1" };
        var first = new Button { Id = "first" };
        var second = new Button { Id = "second" };

        form.Add(first);
        form.Add(second);

        Assert.Equal([first, second], form.Children);
    }

    [Fact]
    public void Add_RejectsAControlThatAlreadyHasAParent()
    {
        var first = new Form { Id = "form1" };
        var second = new Form { Id = "form2" };
        var button = new Button { Id = "btn1" };

        first.Add(button);

        var error = Assert.Throws<InvalidOperationException>(() => second.Add(button));

        Assert.Contains("already belongs", error.Message);
    }

    [Fact]
    public void Add_RejectsSelfParenting()
    {
        var form = new Form { Id = "form1" };

        Assert.Throws<InvalidOperationException>(() => form.Add(form));
    }

    [Fact]
    public void Add_RejectsACycle()
    {
        var grandparent = new Form { Id = "a" };
        var parent = new Form { Id = "b" };
        var child = new Form { Id = "c" };

        grandparent.Add(parent);
        parent.Add(child);

        var error = Assert.Throws<InvalidOperationException>(() => child.Add(grandparent));

        Assert.Contains("cycle", error.Message);
    }

    [Fact]
    public void Remove_DetachesTheChild()
    {
        var form = new Form { Id = "form1" };
        var button = new Button { Id = "btn1" };

        form.Add(button);

        Assert.True(form.Remove(button));
        Assert.Null(button.Parent);
        Assert.Empty(form.Children);
    }

    [Fact]
    public void Remove_ReturnsFalseForAControlItDoesNotOwn()
    {
        var form = new Form { Id = "form1" };

        Assert.False(form.Remove(new Button { Id = "btn1" }));
    }

    [Fact]
    public void RemovedControl_CanBeReparented()
    {
        var first = new Form { Id = "form1" };
        var second = new Form { Id = "form2" };
        var button = new Button { Id = "btn1" };

        first.Add(button);
        first.Remove(button);
        second.Add(button);

        Assert.Same(second, button.Parent);
    }

    [Fact]
    public void ControlType_DefaultsToTheLowercasedTypeName()
    {
        Assert.Equal("button", new Button().ControlType);
        Assert.Equal("textbox", new TextBox().ControlType);
        Assert.Equal("form", new Form().ControlType);
    }
}
