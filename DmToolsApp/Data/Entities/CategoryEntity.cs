using SQLite;

namespace DmToolsApp.Data.Entities
{
    class CategoryEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
