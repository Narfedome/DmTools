using DmToolsApp.Data;
using DmToolsApp.Data.Entities;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class LibraryDataService : ILibraryDataService
    {
        private readonly AppDatabase _db;

        public LibraryDataService(AppDatabase db)
        {
            _db = db;
        }


        public async Task<List<LibraryItem>> GetAllItemsTypeAsync(Type currentLibraryType)
        {

            var result = new List<LibraryItem>();

            if (currentLibraryType == typeof(Track))
            {
                var tracks = await _db.Connection.Table<TrackEntity>().ToListAsync();

                result.AddRange(tracks.Select(t => new Track
                {
                    Id = t.Id,
                    Title = t.Title,
                    ImagePath = t.ImagePath,
                    FilePath = t.FilePath,
                    Duration = t.Duration,
                    Volume = t.DefaultVolume,
                    Hash = t.Hash,
                    Category = t.Category
                }));
            }

            if (currentLibraryType == typeof(Spell))
            {
                var spells = await _db.Connection.Table<SpellEntity>().ToListAsync();

                result.AddRange(spells.Select(s => new Spell
                {
                    Id = s.Id,
                    Title = s.Title,
                    ImagePath = s.ImagePath,
                    FilePath = s.FilePath,
                    Description = s.Description
                }));
            }

            return result.OrderBy(x => x.Id).ToList();
        }

        public async Task SaveLibraryItemAsync(LibraryItem item)
        {
           
            switch (item)
            {
                case Track track:
                    await SaveTrack(track);
                    break;

                case Spell spell:
                    await SaveSpell(spell);
                    break;
            }
        }

        private async Task SaveTrack(Track oldTrack)
        {
            var entity = new TrackEntity
            {
                Id = oldTrack.Id,
                Title = oldTrack.Title,
                ImagePath = oldTrack.ImagePath,
                FilePath = oldTrack.FilePath,
                Duration = oldTrack.Duration,
                DefaultVolume = oldTrack.Volume,
                Hash = oldTrack.Hash,
                Category = oldTrack.Category
            };

            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                oldTrack.Id = entity.Id; // 🔥 IMPORTANT
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }
        public async Task DeleteLibraryItem(LibraryItem libraryItem)
        {
            switch (libraryItem)
            {
                case Track track:
                    await _db.Connection.DeleteAsync<TrackEntity>(track.Id);
                    break;
                case Spell spell:
                    await _db.Connection.DeleteAsync<SpellEntity>(spell.Id);
                    break;

                    // autres types ici
            }
        }

        private async Task SaveSpell(Spell oldSpell)
        {
            var entity = new SpellEntity
            {
                Id = oldSpell.Id,
                Title = oldSpell.Title,
                ImagePath = oldSpell.ImagePath,
                FilePath = oldSpell.FilePath,
                Description = oldSpell.Description
            };
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                oldSpell.Id = entity.Id; // 🔥 IMPORTANT
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }
        public async Task<List<LibraryItem>> GetItemsPageAsync(Type currentLibraryType, int skip, int take, string? category = null)
        {
            var result = new List<LibraryItem>();

            if (currentLibraryType == typeof(Track))
            {
                var query = _db.Connection.Table<TrackEntity>();

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(t => t.Category == category);

                var tracks = await query
                    .OrderBy(t => t.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                result.AddRange(tracks.Select(t => new Track
                {
                    Id = t.Id,
                    Title = t.Title,
                    ImagePath = t.ImagePath,
                    FilePath = t.FilePath,
                    Duration = t.Duration,
                    Volume = t.DefaultVolume,
                    Hash = t.Hash,
                    Category = t.Category
                }));
            }

            if (currentLibraryType == typeof(Spell))
            {
                var spells = await _db.Connection.Table<SpellEntity>()
                    .OrderBy(s => s.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                result.AddRange(spells.Select(s => new Spell
                {
                    Id = s.Id,
                    Title = s.Title,
                    ImagePath = s.ImagePath,
                    FilePath = s.FilePath,
                    Description = s.Description
                }));
            }

            return result;
        }

        public async Task<Track?> FindTrackByHashAsync(string hash, int excludeId)
        {
            if (string.IsNullOrEmpty(hash))
                return null;

            var entity = await _db.Connection.Table<TrackEntity>()
                .Where(t => t.Hash == hash && t.Id != excludeId)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            return new Track
            {
                Id = entity.Id,
                Title = entity.Title,
                ImagePath = entity.ImagePath,
                FilePath = entity.FilePath,
                Duration = entity.Duration,
                Volume = entity.DefaultVolume,
                Hash = entity.Hash,
                Category = entity.Category
            };
        }

        public async Task<List<string>> GetDistinctTrackCategoriesAsync()
        {
            var tracks = await _db.Connection.Table<TrackEntity>().ToListAsync();

            return tracks
                .Select(t => t.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }

        public Task<int> CountTracksWithFilePathAsync(string filePath, int excludeId)
        {
            return _db.Connection.Table<TrackEntity>()
                .Where(t => t.FilePath == filePath && t.Id != excludeId)
                .CountAsync();
        }

        public async Task<HashSet<string>> GetAllReferencedFilePathsAsync()
        {
            var paths = new HashSet<string>();

            var tracks = await _db.Connection.Table<TrackEntity>().ToListAsync();
            foreach (var track in tracks)
            {
                if (!string.IsNullOrEmpty(track.FilePath)) paths.Add(track.FilePath);
                if (!string.IsNullOrEmpty(track.ImagePath)) paths.Add(track.ImagePath);
            }

            var spells = await _db.Connection.Table<SpellEntity>().ToListAsync();
            foreach (var spell in spells)
            {
                if (!string.IsNullOrEmpty(spell.FilePath)) paths.Add(spell.FilePath);
                if (!string.IsNullOrEmpty(spell.ImagePath)) paths.Add(spell.ImagePath);
            }

            return paths;
        }
    }
    public interface ILibraryDataService
    {
        Task SaveLibraryItemAsync(LibraryItem item);
        Task DeleteLibraryItem(LibraryItem item);
        Task<List<LibraryItem>> GetAllItemsTypeAsync(Type currentLibraryType);
        Task<List<LibraryItem>> GetItemsPageAsync(Type currentLibraryType, int skip, int take, string? category = null);
        Task<Track?> FindTrackByHashAsync(string hash, int excludeId);
        Task<int> CountTracksWithFilePathAsync(string filePath, int excludeId);
        Task<HashSet<string>> GetAllReferencedFilePathsAsync();
        Task<List<string>> GetDistinctTrackCategoriesAsync();
    }
}
