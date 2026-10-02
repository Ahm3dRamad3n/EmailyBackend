using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Analytics
{
    public class VolumeByDayDto
    {
        // يجب أن يكون التاريخ بصيغة "yyyy-MM-dd" لأن الفرونت إند بيعمل slice(5) لقص السنة
        public string Date { get; set; } = null!;
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Pending { get; set; }
    }
}
