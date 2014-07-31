using System.Runtime.Serialization;

namespace MeetWhere.Cloud
{
    [DataContract(Name = "MapMetadata")]
    public class MapMetadata
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "building")]
        public int Building { get; set; }

        [DataMember(Name = "floor")]
        public int Floor { get; set; }

        [DataMember(Name = "lat")]
        public double CenterLat { get; set; }

        [DataMember(Name = "long")]
        public double CenterLong { get; set; }

        [DataMember(Name = "mapsize")]
        public int MapSize { get; set; }
        
        [DataMember(Name = "zoomlevel")]
        public int ZoomLevel { get; set; }

        [DataMember(Name = "angle")]
        public double Angle { get; set; }

        [DataMember(Name = "scale")]
        public double Scale { get; set; }

        [DataMember(Name = "offsetx")]
        public double OffsetX { get; set; }

        [DataMember(Name = "offsety")]
        public double OffsetY { get; set; }
    }
}
