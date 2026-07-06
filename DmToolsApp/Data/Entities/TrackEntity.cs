using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    class TrackEntity : LibraryItemEntity
    {

        public TimeSpan Duration { get; set; }

        public double DefaultVolume { get; set; }

        public string Hash { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}
