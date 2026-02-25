using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DmToolsApp.Models
{
    public partial class Character : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Spell> spells = new ObservableCollection<Spell>();
    }
}
