using System.Runtime.Serialization;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   [DataContract]
   public class TicketStatusChange
   {
      [DataMember]
      public int TicketID { get; set; }
      [DataMember]
      public int Status { get; set; }
      [DataMember]
      public string UserId { get; set; }
      [DataMember]
      public string BrandGUID { get; set; }
   }
}
