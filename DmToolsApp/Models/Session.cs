using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DmToolsApp.Models
{
   public partial class Session : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Scene> scenes = new ObservableCollection<Scene>();
        public int Id { get; set; }
    }
}
