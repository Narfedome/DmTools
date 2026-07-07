using DmToolsApp.Data.Entities;
using DmToolsApp.Services;
using SQLite;

namespace DmToolsApp.Data
{
    public class AppDatabase
    {
        public readonly SQLiteAsyncConnection _db;
        public SQLiteAsyncConnection Connection => _db;

        public AppDatabase(string path)
        {
            _db = new SQLiteAsyncConnection(path);
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await _db.CreateTableAsync<CampaignEntity>();
            await _db.CreateTableAsync<SessionEntity>();
            await _db.CreateTableAsync<SceneEntity>();
            await _db.CreateTableAsync<CharacterEntity>();
            await _db.CreateTableAsync<SpellEntity>();
            await _db.CreateTableAsync<TrackEntity>();
            await _db.CreateTableAsync<SceneTrackEntity>();
            await _db.CreateTableAsync<CharacterSpellEntity>();
            await _db.CreateTableAsync<CategoryEntity>();

            // Migration : ajout de IsLooping sur les DB existantes
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE SceneTrackEntity ADD COLUMN IsLooping INTEGER NOT NULL DEFAULT 1");
            }
            catch { /* colonne déjà présente */ }

            // Migration : ajout de Hash sur les DB existantes
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE TrackEntity ADD COLUMN Hash TEXT NOT NULL DEFAULT ''");
            }
            catch { /* colonne déjà présente */ }

            // Migration : ajout de Category sur les DB existantes
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE TrackEntity ADD COLUMN Category TEXT NOT NULL DEFAULT ''");
            }
            catch { /* colonne déjà présente */ }

            // Seed initial des catégories (une seule fois, table vide) : les 3 catégories par défaut
            // + toute catégorie déjà utilisée par des tracks existantes (upgrade depuis une DB qui
            // n'avait pas encore cette table). Si l'utilisateur en supprime une ensuite, elle ne
            // revient pas au prochain lancement.
            if (await _db.Table<CategoryEntity>().CountAsync() == 0)
            {
                var defaults = new[]
                {
                    LocalizationService.Instance["LibCategoryMusic"],
                    LocalizationService.Instance["LibCategoryAmbience"],
                    LocalizationService.Instance["LibCategorySoundEffect"]
                };

                var existingTrackCategories = (await _db.Table<TrackEntity>().ToListAsync())
                    .Select(t => t.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c));

                foreach (var name in defaults.Union(existingTrackCategories, StringComparer.OrdinalIgnoreCase))
                    await _db.InsertAsync(new CategoryEntity { Name = name });
            }
        }
    }
}
