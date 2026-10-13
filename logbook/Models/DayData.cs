using System.Collections.Generic;

namespace logbook.Models
{
    public class DayData
    {
        public string ExchangeId { get; set; } = "";

        public string Date { get; set; }
        public string Profile { get; set; }
        public string Crew { get; set; }
        public List<Launch> Launches { get; set; } = new List<Launch>();
        public List<SnkValue> SnkValues { get; set; } = new List<SnkValue>();
    }
}