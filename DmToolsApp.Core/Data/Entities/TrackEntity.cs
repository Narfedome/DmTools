using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace DmToolsApp.Data.Entities
{
    public class TrackEntity : LibraryItemEntity
    {

        public TimeSpan Duration { get; set; }

        public double DefaultVolume { get; set; }

        public string Hash { get; set; } = string.Empty;

        // Indexé : GetItemsPageAsync trie désormais par (Category, Id) pour afficher la bibliothèque
        // groupée par catégorie (LibraryTrackViewModel) - sans index, ce tri dégrade en scan complet de
        // la table à chaque page chargée au lieu de profiter de l'ordre déjà natif sur Id (clé primaire).
        [Indexed]
        public string Category { get; set; } = string.Empty;
    }
}
