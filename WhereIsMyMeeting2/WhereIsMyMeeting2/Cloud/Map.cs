using System.Runtime.Serialization;

namespace MeetWhere.Cloud
{
    public class Map
    {
        public int Id { get; set; }

        [DataMember(Name = "building")]
        public int Building { get; set; }

        [DataMember(Name = "floor")]
        public int Floor { get; set; }

        [DataMember(Name = "part")]
        public int Part { get; set; }

        [DataMember(Name = "svg")]
        public string SVG { get; set; }
    }
}
