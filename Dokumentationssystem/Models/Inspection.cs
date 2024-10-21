using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dokumentationssystem.Models
{
    namespace Dokumentationssystem.Models
    {
        public class Inspection
        {
            public int Id { get; set; }
            public string InspectionName { get; set; }
            public string Address { get; set; }
            public DateTime Date { get; set; }
            public string CreatedBy { get; set; }
        }
    }

}
