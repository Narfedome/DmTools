using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    public class SceneEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SessionId { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}
