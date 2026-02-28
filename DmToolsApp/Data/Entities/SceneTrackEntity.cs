using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
   public  class SceneTrackEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SceneId { get; set; }

        public int TrackId { get; set; }

        public double Volume { get; set; } = 1.0;

        public int Position { get; set; }
    }
}
