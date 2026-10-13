using System;
using System.Collections.Generic;

namespace logbook.Models
{
    public class DailyRecord
    {
        public DateTime Date { get; set; }
        public string Note { get; set; } = "";
        public List<Launch> Launches { get; set; } = new List<Launch>();
    }
}