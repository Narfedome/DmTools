using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Models.Library
{
    public partial class Spell : LibraryItem
    {
        [ObservableProperty]
        private string description = "";
    }
}
