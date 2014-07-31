#if WINDOWS_PHONE
using Microsoft.Phone.UserData;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetWhere.XPlat
{
    public class FakeAppointment
    {
        public FakeAppointment(string subject, DateTime startTime, string location)
        {
            this.Subject = subject;
            this.StartTime = startTime;
            this.Location = location;
        }

#if WINDOWS_PHONE
        public FakeAppointment(Appointment realAppt)
        {
            this.Location = realAppt.Location;
            this.StartTime = realAppt.StartTime;
            this.Subject = realAppt.Subject;
        }
#endif

        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public string Subject { get; set; }

    }
}
