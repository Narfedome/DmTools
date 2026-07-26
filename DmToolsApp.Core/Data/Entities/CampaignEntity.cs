using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    public class CampaignEntity : IPositioned
    {
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public int Position { get; set; }
    }
}
