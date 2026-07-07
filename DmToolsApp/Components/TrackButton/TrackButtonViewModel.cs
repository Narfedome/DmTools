using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Microsoft.Maui.Graphics.Platform;
using System.Collections.Concurrent;
using System.ComponentModel;
using DmToolsApp;

namespace DmToolsApp.Components.TrackButton
{
    public partial class TrackButtonViewModel : BaseViewModel
    {
        // Taille max (px) des vignettes de pochette - largement suffisant pour l'affichage 120x120,
        // avec un peu de marge pour les écrans haute densité, sans garder les images en pleine résolution.
        private const int CoverThumbnailSize = 160;

        // Limite le nombre d'extractions TagLib simultanées : sans ça, ouvrir la bibliothèque lance
        // 12 parsings de fichiers (potentiellement volumineux) en même temps, ce qui sature le
        // thread pool et le disque au lieu de charger les pochettes en douceur.
        private static readonly SemaphoreSlim CoverLoadThrottle = new(3);

        // Evite de re-parser le fichier (TagLib) à chaque affichage/scroll de la même track
        private static readonly ConcurrentDictionary<string, ImageSource?> CoverImageCache = new();

        private readonly AudioPlayerService _audioService;

        public TrackButtonViewModel(AudioPlayerService audioService)
        {
            _audioService = audioService;

            _audioService.OnStateChanged += OnAudioChanged;
        }

        private void OnAudioChanged(string? currentFile)
        {
            IsPlaying = CurrentTrack != null && currentFile == CurrentTrack.FilePath;
        }

        [ObservableProperty]
        private Track? currentTrack;
        partial void OnCurrentTrackChanged(Track? oldValue, Track? newValue)
        {
            if (oldValue != null)
                oldValue.PropertyChanged -= OnTrackChanged;

            if (newValue != null)
                newValue.PropertyChanged += OnTrackChanged;

            UpdateFromTrack(newValue);
        }
        private void OnTrackChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateFromTrack(CurrentTrack);
        }

        private void UpdateFromTrack(Track? value)
        {
            if (value == null)
            {
                CoverImage = null;
                IsPlaying = false;
                IsLoadingCover = false;
                return;
            }

            IsPlaying = _audioService.CurrentFile == value.FilePath;

            if (CoverImageCache.TryGetValue(value.FilePath, out var cached))
            {
                CoverImage = cached;
                IsLoadingCover = false;
                return;
            }

            CoverImage = null;
            IsLoadingCover = true;
            _ = LoadCoverImageAsync(value);
        }

        private async Task LoadCoverImageAsync(Track track)
        {
            var image = await LoadAndCacheCoverAsync(track.FilePath);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (CurrentTrack == track)
                {
                    CoverImage = image;
                    IsLoadingCover = false;
                }
            });
        }

        /// <summary>
        /// Charge (ou lit depuis le cache) la pochette d'une piste. Utilisé aussi bien par la tuile
        /// elle-même (chargement paresseux) que par la liste (préchargement d'une page avant d'afficher
        /// les tuiles, pour que l'indicateur de chargement de la liste couvre toute l'attente).
        /// </summary>
        public static async Task<ImageSource?> LoadAndCacheCoverAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (CoverImageCache.TryGetValue(filePath, out var cached))
                return cached;

            await CoverLoadThrottle.WaitAsync();
            try
            {
                var image = await Task.Run(() => GetCoverImage(filePath));
                CoverImageCache[filePath] = image;
                return image;
            }
            finally
            {
                CoverLoadThrottle.Release();
            }
        }

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLoadingCover;

        [ObservableProperty]
        private ImageSource? coverImage;

        [RelayCommand]
        private void TogglePlay()
        {
            if (CurrentTrack == null)
                return;

            _audioService.Toggle(CurrentTrack.FilePath);
        }

        public static ImageSource? GetCoverImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var file = TagLib.File.Create(filePath);

            if (file.Tag.Pictures.Length == 0)
                return null;

            var pic = file.Tag.Pictures[0];
            var thumbnail = CreateThumbnail(pic.Data.Data) ?? pic.Data.Data;
            return ImageSource.FromStream(() => new MemoryStream(thumbnail));
        }

        /// <summary>
        /// Redimensionne la pochette embarquée (souvent bien plus grande que nécessaire) en une petite
        /// vignette JPEG, pour éviter de garder des images pleine résolution en mémoire pour un
        /// affichage de 120x120. Retourne null si le redimensionnement échoue (format non supporté...),
        /// auquel cas l'appelant garde l'image d'origine.
        /// </summary>
        private static byte[]? CreateThumbnail(byte[] originalBytes)
        {
            try
            {
                using var inputStream = new MemoryStream(originalBytes);
                using var image = PlatformImage.FromStream(inputStream);
                using var resized = image.Downsize(CoverThumbnailSize, disposeOriginal: false);
                using var outputStream = new MemoryStream();
                resized.Save(outputStream, ImageFormat.Jpeg, 0.85f);
                return outputStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

    }
}
