using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signify.ElasticGrafanaPoc
{
    public class SigEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public DateTime Date { get; set; }
        public string Product { get; set; }
        public Guid Guid { get; set; }
    }
}
