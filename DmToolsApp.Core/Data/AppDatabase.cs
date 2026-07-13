using DmToolsApp.Data.Entities;
using DmToolsApp.Models.Library;
using SQLite;

namespace DmToolsApp.Data
{
    public class AppDatabase
    {
        public readonly SQLiteAsyncConnection _db;
        public SQLiteAsyncConnection Connection => _db;

        private readonly IReadOnlyList<string> _defaultTrackCategories;

        /// <summary>
        /// Création des tables, migrations et seed, lancés dès la construction. L'appli n'attend pas
        /// (les premières requêtes SQLite s'enchaînent sur la même connexion), mais les tests peuvent
        /// await pour un état déterministe.
        /// </summary>
        public Task Initialization { get; }

        /// <param name="defaultTrackCategories">Catégories de tracks seedées au premier lancement
        /// (localisées par l'appelant — la couche données ne dépend pas de la localisation).</param>
        public AppDatabase(string path, IEnumerable<string>? defaultTrackCategories = null)
        {
            _defaultTrackCategories = defaultTrackCategories?.ToList() ?? new List<string>();
            _db = new SQLiteAsyncConnection(path);
            Initialization = InitializeAsync();
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

            // Migration : ajout de FadeIn sur les DB existantes
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE SceneTrackEntity ADD COLUMN FadeIn INTEGER NOT NULL DEFAULT 0");
            }
            catch { /* colonne déjà présente */ }

            // Migration : ajout de FadeOut sur les DB existantes
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE SceneTrackEntity ADD COLUMN FadeOut INTEGER NOT NULL DEFAULT 0");
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

            // Migration : ajout de LibraryType sur les DB existantes (catégories scopées par type)
            try
            {
                await _db.ExecuteAsync(
                    "ALTER TABLE CategoryEntity ADD COLUMN LibraryType TEXT NOT NULL DEFAULT ''");
            }
            catch { /* colonne déjà présente */ }

            // Toute catégorie créée avant l'introduction de LibraryType ne pouvait être que pour des
            // tracks (les sorts n'ont pas encore de catégories) : on les rattache rétroactivement.
            await _db.ExecuteAsync(
                $"UPDATE CategoryEntity SET LibraryType = '{nameof(Track)}' WHERE LibraryType = ''");

            // Seed initial des catégories de tracks (une seule fois, aucune catégorie de ce type) : les
            // 3 catégories par défaut + toute catégorie déjà utilisée par des tracks existantes (upgrade
            // depuis une DB qui n'avait pas encore cette table). Si l'utilisateur en supprime une
            // ensuite, elle ne revient pas au prochain lancement.
            if (await _db.Table<CategoryEntity>().Where(c => c.LibraryType == nameof(Track)).CountAsync() == 0)
            {
                var existingTrackCategories = (await _db.Table<TrackEntity>().ToListAsync())
                    .Select(t => t.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c));

                foreach (var name in _defaultTrackCategories.Union(existingTrackCategories, StringComparer.OrdinalIgnoreCase))
                    await _db.InsertAsync(new CategoryEntity { Name = name, LibraryType = nameof(Track) });
            }
        }
    }
}
