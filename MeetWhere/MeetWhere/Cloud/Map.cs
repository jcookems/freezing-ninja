using System.Runtime.Serialization;

namespace MeetWhere.Cloud
{
    [DataContract(Name = "Map")]
    public class Map
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "building")]
        public int Building { get; set; }

        [DataMember(Name = "floor")]
        public int Floor { get; set; }

        [DataMember(Name = "part")]
        public int Part { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}
