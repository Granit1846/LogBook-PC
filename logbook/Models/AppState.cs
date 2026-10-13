using System.Collections.Generic;

namespace logbook.Models
{
    public class AppState
    {
        // Дата и профиль
        public string SelectedProfile { get; set; }
        public System.DateTime SelectedDate { get; set; }

        // День
        public string Crew { get; set; }
        public List<Launch> Launches { get; set; } = new List<Launch>();
        public List<SnkValue> SnkValues { get; set; } = new List<SnkValue>();

        // Формуляры
        public List<Chast1Record> Chast1Records { get; set; } = new List<Chast1Record>();
        public Chast1Record Chast1StartRecord { get; set; } = new Chast1Record();

        public List<Vr80Record> Vr80Records { get; set; } = new List<Vr80Record>();
        public Vr80Record Vr80StartRecord { get; set; } = new Vr80Record();

        public List<Ta14Record> Ta14Records { get; set; } = new List<Ta14Record>();
        public Ta14Record Ta14StartRecord { get; set; } = new Ta14Record();

        public List<Vk250001Record> Vk250001Records { get; set; } = new List<Vk250001Record>();
        public Vk250001Record Vk250001StartRecord { get; set; } = new Vk250001Record();
        public string Vk250001CoeffA { get; set; } = "";
        public string Vk250001CoeffH { get; set; } = "";

        public List<Vk250002Record> Vk250002Records { get; set; } = new List<Vk250002Record>();
        public Vk250002Record Vk250002StartRecord { get; set; } = new Vk250002Record();
        public string Vk250002CoeffA { get; set; } = "";
        public string Vk250002CoeffH { get; set; } = "";

        public List<ResourceRecord> ResourceRecords { get; set; } = new List<ResourceRecord>();
        public List<OilRecord> OilRecords { get; set; } = new List<OilRecord>();
    }
}