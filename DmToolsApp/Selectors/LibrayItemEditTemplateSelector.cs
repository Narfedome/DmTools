using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Selectors
{
    public class LibraryItemEditTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TrackTemplate { get; set; } = new DataTemplate();
        public DataTemplate SpellTemplate { get; set; } = new DataTemplate();

        protected override DataTemplate OnSelectTemplate(
            object item,
            BindableObject container)
        {
            return item switch
            {
                Track => TrackTemplate,
                Spell => SpellTemplate,
                _ => TrackTemplate
            };
        }
    }
}
