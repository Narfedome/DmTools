using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryItemEditViewModel
    : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTrack))]
        [NotifyPropertyChangedFor(nameof(IsSpell))]
        private LibraryItem? item;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Item", out var value) &&
           value is LibraryItem item)
            {
                Item = item;
            }
        }

        public bool IsTrack => Item is Track;
        public bool IsSpell => Item is Spell;

        [RelayCommand]
        public async Task Save()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
