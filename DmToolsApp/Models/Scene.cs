using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DmToolsApp.Models
{
    public partial class Scene : ObservableObject
    {

        [ObservableProperty]
        private ObservableCollection<Track> tracks = new ObservableCollection<Track>();
    }
}
