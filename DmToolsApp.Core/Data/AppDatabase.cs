using DmToolsApp.Data.Entities;
using DmToolsApp.Models.Library;
using SQLite;

namespace DmToolsApp.Data
{
    public class AppDatabase
    {
        private readonly SQLiteAsyncConnection _db;
        public SQLiteAsyncConnection Connection => _db;

        private readonly IReadOnlyList<string> _defaultTrackCategories;

        /// <summary>
        /// Création des tables, migrations et seed, lancés dès la construction. Les services de
        /// données l'attendent en tête de chaque méthode : SQLiteAsyncConnection ne garantit pas
        /// l'ordre d'exécution entre opérations, une requête partie avant la fin de l'init pouvait
        /// tomber sur "no such table" au premier lancement.
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

            // Migrations : ajout des colonnes manquantes sur les DB existantes. Le catch ne cible
            // que l'erreur "duplicate column" (colonne déjà présente) : un vrai problème (disque
            // plein, DB corrompue...) doit remonter au lieu d'être avalé silencieusement, sans quoi
            // l'appli planterait plus loin de façon incompréhensible.
            await AddColumnIfMissingAsync("SceneTrackEntity", "IsLooping INTEGER NOT NULL DEFAULT 1");
            await AddColumnIfMissingAsync("SceneTrackEntity", "FadeIn INTEGER NOT NULL DEFAULT 0");
            await AddColumnIfMissingAsync("SceneTrackEntity", "FadeOut INTEGER NOT NULL DEFAULT 0");
            await AddColumnIfMissingAsync("TrackEntity", "Hash TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync("TrackEntity", "Category TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync("CategoryEntity", "LibraryType TEXT NOT NULL DEFAULT ''");

            // Purge unique des lignes orphelines laissées par les suppressions sans cascade des
            // versions précédentes (supprimer une campagne ne supprimait ni ses chapitres, ni
            // leurs scènes, ni leurs pistes). L'ordre importe : parents d'abord.
            await _db.ExecuteAsync("DELETE FROM SessionEntity WHERE CampaignId NOT IN (SELECT Id FROM CampaignEntity)");
            await _db.ExecuteAsync("DELETE FROM SceneEntity WHERE SessionId NOT IN (SELECT Id FROM SessionEntity)");
            await _db.ExecuteAsync("DELETE FROM SceneTrackEntity WHERE SceneId NOT IN (SELECT Id FROM SceneEntity)");
            await _db.ExecuteAsync("DELETE FROM SceneTrackEntity WHERE TrackId NOT IN (SELECT Id FROM TrackEntity)");

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

        private async Task AddColumnIfMissingAsync(string table, string columnDefinition)
        {
            try
            {
                await _db.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {columnDefinition}");
            }
            catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
            {
                // Colonne déjà présente (DB déjà migrée) : rien à faire.
            }
        }
    }
}
