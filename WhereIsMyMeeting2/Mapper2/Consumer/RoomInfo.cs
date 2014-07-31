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
        public int Building { get; private set; }
        public int Floor { get; private set; }
        public int Room { get; private set; }
        public string Location { get; private set; }

        private RoomInfo(int building, int room)
        {
            this.Building = building;
            this.Floor = ToInt(room.ToString()[0].ToString());
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
                    var building = ToInt(roomComponents[0]);
                    var room = ToInt(roomComponents[1]);
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
                    return new RoomInfo(ToInt(currentBuilding), ToInt(numbers[0]));
                }
                else if (numbers.Count == 2)
                {
                    return new RoomInfo(ToInt(numbers[0]), ToInt(numbers[1]));
                }
            }

            MessageBox.Show("Must give room name of the form 'buildingNumber/roomNumber' when no map is shown, but got '" + rawLocation + "'",
                "Location",
                MessageBoxButton.OK);
            return null;
        }

        public override string ToString()
        {
            return Building + "/" + Room;
        }

        private static int ToInt(string s)
        {
            int tmp;
            if (!int.TryParse(s, out tmp))
            {
                throw new Exception("Cannot parse '" + s + "' as integer");
            }
            return tmp;
        }
    }
}
