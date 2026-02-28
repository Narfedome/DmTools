using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    public class CharacterEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
