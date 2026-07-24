using DmToolsApp.Components;
using System.ComponentModel;

namespace DmToolsApp.Features.AudioMixer
{
    // Sauvegarde debouncée des réglages Volume/IsLooping d'un ChannelStripViewModel : regroupe les
    // changements rapprochés (ex. drag d'un slider) derrière un seul délai au lieu d'écrire en base
    // à chaque valeur intermédiaire. Extrait d'AudioMixerViewModel pour l'isoler de la logique de
    // scène/session, qui n'a rien à voir avec ce debounce.
    internal sealed class ChannelAutoSaveScheduler
    {
        private readonly Func<ChannelStripViewModel, Task> _save;
        private readonly Func<ChannelStripViewModel, bool> _shouldSave;
        private readonly TimeSpan _delay;
        private readonly Dictionary<ChannelStripViewModel, CancellationTokenSource> _pending = new();

        public ChannelAutoSaveScheduler(
            Func<ChannelStripViewModel, Task> save,
            Func<ChannelStripViewModel, bool> shouldSave,
            TimeSpan? delay = null)
        {
            _save = save;
            _shouldSave = shouldSave;
            _delay = delay ?? TimeSpan.FromMilliseconds(500);
        }

        public void Track(ChannelStripViewModel channel)
        {
            channel.PropertyChanged -= OnChannelPropertyChanged;
            channel.PropertyChanged += OnChannelPropertyChanged;
        }

        public void Untrack(ChannelStripViewModel channel)
        {
            channel.PropertyChanged -= OnChannelPropertyChanged;
            CancelPending(channel);
        }

        public void CancelPending(ChannelStripViewModel channel)
        {
            if (_pending.TryGetValue(channel, out var cts))
            {
                cts.Cancel();
                _pending.Remove(channel);
            }
        }

        private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ChannelStripViewModel channel) return;
            if (e.PropertyName is nameof(ChannelStripViewModel.Volume) or nameof(ChannelStripViewModel.IsLooping))
                _ = ScheduleAsync(channel);
        }

        private async Task ScheduleAsync(ChannelStripViewModel channel)
        {
            if (!_shouldSave(channel)) return;

            if (_pending.TryGetValue(channel, out var existingCts))
                existingCts.Cancel();

            var cts = new CancellationTokenSource();
            _pending[channel] = cts;

            try
            {
                await Task.Delay(_delay, cts.Token);
                await _save(channel);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_pending.TryGetValue(channel, out var current) && current == cts)
                    _pending.Remove(channel);
                cts.Dispose();
            }
        }
    }
}
