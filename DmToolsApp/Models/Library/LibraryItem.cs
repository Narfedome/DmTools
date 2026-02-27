using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Models.Library
{
    public class LibraryItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = ""; 
        public string ImagePath { get; set; } = "";
    }

    public enum LibraryType
    {
        SoundEffects,
        Music,
        Images,
        Tokens
    }
}
