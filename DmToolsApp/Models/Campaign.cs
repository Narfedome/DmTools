using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DmToolsApp.Models
{
    public partial class Campaign : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Session> sessions = new ObservableCollection<Session>();

    }
}
