using Microsoft.WindowsAzure.Mobile.Service;
using System;

namespace SmsHelperService.DataObjects
{
    public class TodoItem : EntityData
    {
        public string Text { get; set; }
        public string To { get; set; }
        public string From { get; set; }

        public DateTimeOffset Sent { get; set; }
    }
}