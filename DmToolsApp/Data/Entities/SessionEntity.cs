using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Data.Entities
{
    public class SessionEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int CampaignId { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}
