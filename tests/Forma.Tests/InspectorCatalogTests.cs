using Forma.Builder;

namespace Forma.Tests;

public class InspectorCatalogTests
{
    [Fact]
    public void ContextualEditors_SeparateFormButtonAndTextBoxProperties()
    {
        var form = InspectorCatalog.ForKind("form").ToArray();
        var button = InspectorCatalog.ForKind("button").ToArray();
        var textbox = InspectorCatalog.ForKind("textbox").ToArray();
        Assert.DoesNotContain(form, p => p.Id == "x" || p.Id == "marginTop");
        Assert.Contains(form, p => p.Id == "width");
        Assert.Contains(button, p => p.Id == "style");
        Assert.DoesNotContain(textbox, p => p.Id == "style");
        Assert.Contains(textbox, p => p.Id == "readOnly");
        Assert.DoesNotContain(button, p => p.Id == "readOnly");
    }

    [Fact]
    public void StableIdentity_IsReadOnlyInEveryCategory()
    {
        Assert.All(InspectorCatalog.Properties.Where(p => p.Id == "id"), p => Assert.True(p.ReadOnly));
        Assert.Equal(2, InspectorCatalog.Properties.Count(p => p.Id == "id"));
    }

    [Fact]
    public void DescriptorBoundsAndChoices_AreValid()
    {
        foreach (var property in InspectorCatalog.Properties)
        {
            Assert.False(string.IsNullOrWhiteSpace(property.Category));
            if (property.Min.HasValue && property.Max.HasValue) Assert.True(property.Min <= property.Max);
            if (property.Editor == "select") Assert.NotEmpty(property.Options!);
        }
    }
}
