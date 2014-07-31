using System.Runtime.Serialization;

namespace MeetWhere.Cloud
{
    class AppUser
    {
        public int Id { get; set; }

        [DataMember(Name = "userId")]
        public string UserId { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "isAuthorized")]
        public bool IsAuthorized { get; set; }
    }
}
