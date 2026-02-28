using DmToolsApp.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data
{
    public class AppDatabase
    {
        public readonly SQLiteAsyncConnection _db;
        public SQLiteAsyncConnection Connection => _db;

        public AppDatabase(string path)
        {
            _db = new SQLiteAsyncConnection(path);

            _db.CreateTableAsync<CampaignEntity>();
            _db.CreateTableAsync<SessionEntity>();
            _db.CreateTableAsync<SceneEntity>();
            _db.CreateTableAsync<CharacterEntity>();
            _db.CreateTableAsync<SpellEntity>();
            _db.CreateTableAsync<TrackEntity>();

            _db.CreateTableAsync<SceneTrackEntity>();
            _db.CreateTableAsync<CharacterSpellEntity>();
        }
    }
}
