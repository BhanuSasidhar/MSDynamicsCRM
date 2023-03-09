using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   [DataContract]
   public class TicketAssignmentChange
   {
      [DataMember]
      public int TicketID { get; set; }
      [DataMember]
      public int AssignedToUserID { get; set; }
      [DataMember]
      public string UserId { get; set; }
      [DataMember]
      public string BrandGUID { get; set; }
   }
}
