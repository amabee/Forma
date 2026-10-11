namespace Forma.Builder;

// Autosave for crash recovery: unsaved work is written to a recovery file every
// 30 seconds and offered back the next time the Builder starts after a crash.
public sealed partial class BuilderWindow
{
    private static readonly TimeSpan AutosaveInterval = TimeSpan.FromSeconds(30);
    private RecoverySession? _recovery;
    private System.Windows.Forms.Timer? _autosave;

    private async Task StartRecoveryAsync()
    {
        try { _recovery = RecoveryStore.Default.Begin(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            await StateAsync($"Autosave is unavailable: {error.Message}");
            return;
        }
        _autosave = new System.Windows.Forms.Timer { Interval = (int)AutosaveInterval.TotalMilliseconds };
        _autosave.Tick += (_, _) => Autosave();
        _autosave.Start();
        await OfferRecoveryAsync();
    }

    private void Autosave()
    {
        if (_recovery is null || !_ready || _viewModel.Form is null) return;
        try
        {
            if (_viewModel.IsMarkedUnsaved) _recovery.Save(_viewModel.CaptureProject(embedImages: true), _viewModel.ProjectPath);
            else _recovery.Clear();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _ = StateAsync($"Autosave failed: {error.Message}");
        }
    }

    private async Task OfferRecoveryAsync()
    {
        var entry = RecoveryStore.Default.FindAbandoned().FirstOrDefault();
        if (entry is null) return;
        var name = entry.ProjectPath is null ? "an untitled design" : Path.GetFileName(entry.ProjectPath);
        var answer = MessageBox.Show(this,
            $"Forma Builder didn't close normally. Restore unsaved changes to {name} from {entry.SavedAtUtc.ToLocalTime():g}?\n\n"
                + "Choose No to discard the recovered changes.",
            "Recover unsaved work", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        // Cancel keeps the recovery file so it is offered again next time.
        if (answer == DialogResult.Cancel) return;
        if (answer == DialogResult.Yes)
        {
            try
            {
                if (!await LoadProjectAsync(entry.DocumentPath, entry.ProjectPath, saved: false)) return;
                await StateAsync($"Recovered unsaved changes to {name}. Save to keep them.");
            }
            catch (Exception error)
            {
                _ready = true;
                MessageBox.Show(this, error.Message, "Could not recover design", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        RecoveryStore.Default.Discard(entry.Id);
    }

    private void StopRecovery()
    {
        _autosave?.Dispose();
        _autosave = null;
        // A normal close has already asked about unsaved changes, so the autosave is no longer needed.
        _recovery?.Dispose();
        _recovery = null;
    }
}
