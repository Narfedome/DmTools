using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    class TrackEntity : LibraryItemEntity
    {
        public string FilePath { get; set; } = string.Empty;

        public TimeSpan Duration { get; set; }

        public double DefaultVolume { get; set; }
    }
}
