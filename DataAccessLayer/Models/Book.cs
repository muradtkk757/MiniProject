using DataAccessLayer.Models;

namespace DataAccessLayerModels
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public int PublishedYear { get; set; }
        public int CategoryId { get; set; }
        public bool IsAvailable { get; set; }

        public int CurrentMemberId { get; set; }

    }
}


