using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskService.Models
{
    // Models/User.cs
    public class User
    {
        //[BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
        //public string Id { get; set; } = null!;
        //public string _Id { get; set; } = null!;
        //public string Username { get; set; } = null!;
        //public string Role { get; set; } =null!;

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("Password")]
        public string Password { get; set; }

        [BsonElement("Role")]
        public string Role { get; set; }
    }
}
