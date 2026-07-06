using DmToolsApp.Data.Entities;
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
        }
    }
}
