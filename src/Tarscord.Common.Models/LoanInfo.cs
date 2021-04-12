using System;

namespace Tarscord.Common.Models
{
    public class LoanInfo: BaseModel
    {
        public DateTime? EventDate { get; set; }

        public string EventDescription { get; set; }

        public bool IsActive { get; set; }
    }
}