using Forma.Builder;

namespace Forma.Tests;

public class ComponentSaveSessionTests
{
    [Fact]
    public async Task FirstSaveAppliesBoilerplateAndRepeatedFileNotificationsDoNotReapply()
    {
        var source = new ComponentCustomization { Css = ":host { color: blue; }" };
        var commits = 0;
        var session = new ComponentSaveSession(source, _ => { commits++; return Task.CompletedTask; });
        Assert.True(await session.ApplyAsync(source));
        Assert.False(await session.ApplyAsync(source));
        Assert.Equal(1, commits);
        Assert.True(await session.ApplyAsync(new() { Css = ":host { color: red; }" }));
        Assert.Equal(2, commits);
        Assert.Contains("red", session.Current.Css);
    }

    [Fact]
    public async Task InvalidAndFailedSavesRetainTheLastAppliedVersionAndCanBeRetried()
    {
        var fail = false;
        var session = new ComponentSaveSession(new(), _ => fail ? Task.FromException(new IOException("Save failed")) : Task.CompletedTask);
        var valid = new ComponentCustomization { Characteristics = "{\"count\":1}" };
        await session.ApplyAsync(valid);
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => session.ApplyAsync(new() { Characteristics = "{" }));
        Assert.Equal(valid.Characteristics, session.Current.Characteristics);
        fail = true;
        var changed = new ComponentCustomization { Characteristics = "{\"count\":2}" };
        await Assert.ThrowsAsync<IOException>(() => session.ApplyAsync(changed));
        Assert.False(session.IsCurrent(changed));
        Assert.Equal(valid.Characteristics, session.Current.Characteristics);
        fail = false;
        Assert.True(await session.ApplyAsync(changed));
    }
}
