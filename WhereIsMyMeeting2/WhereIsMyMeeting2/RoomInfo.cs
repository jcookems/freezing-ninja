using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WhereIsMyMeeting2
{
    public class RoomInfo
    {
        public string Building { get; private set; }
        public string Floor { get; private set; }
        public string Room { get; private set; }
        public string Location { get; private set; }

        private RoomInfo(string building, string room)
        {
            this.Building = building;
            this.Floor = room[0].ToString();
            this.Room = room;
            this.Location = building + "/" + room;
        }

        public static RoomInfo Parse(string rawLocation, string currentBuilding)
        {
            if (rawLocation != null)
            {
                var match = Regex.Match(rawLocation, @"\d+/\d+");
                if (match.Success)
                {
                    var roomComponents = match.Value.Split('/');
                    var building = roomComponents[0];
                    var room = roomComponents[1];
                    return new RoomInfo(building, room);
                }

                match = Regex.Match(rawLocation, @"\d+");
                List<string> numbers = new List<string>();
                while (match.Success)
                {
                    numbers.Add(match.Value);
                    match = match.NextMatch();
                }

                if (numbers.Count == 1 && !string.IsNullOrEmpty(currentBuilding))
                {
                    // Assume room in current building
                    return new RoomInfo(currentBuilding, numbers[0]);
                }
                else if (numbers.Count == 2)
                {
                    return new RoomInfo(numbers[0], numbers[1]);
                }
            }

            MessageBox.Show("Must give room name of the form 'buildingNumber/roomNumber' when no map is shown, but got '" + rawLocation + "'",
                "Location",
                MessageBoxButton.OK);
            return null;
        }
    }
}
