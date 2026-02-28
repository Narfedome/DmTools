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
                    Volume = t.DefaultVolume
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
                DefaultVolume = oldTrack.Volume
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
                Title = oldSpell.Title,
                ImagePath = oldSpell.ImagePath,
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
    }
    public interface ILibraryDataService
    {
        Task SaveLibraryItemAsync(LibraryItem item);
        Task DeleteLibraryItem(LibraryItem item);
        Task<List<LibraryItem>> GetAllItemsTypeAsync(Type currentLibraryType);
    }
}
