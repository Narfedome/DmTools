using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Selectors
{
    public class LibraryItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SpellTemplate { get; set; }
        public DataTemplate TrackTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                Spell => SpellTemplate,
                Track => TrackTemplate,
                _ => SpellTemplate
            };
        }
    }
}
