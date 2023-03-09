using System.Runtime.Serialization;

namespace proMX.Locobuzz.Plugins
{
   [DataContract]
   public class UiJson
   {
      [DataMember]
      public bool StatusSync { get; set; }
      [DataMember]
      public bool NoteSync { get; set; }
      [DataMember]
      public bool AssignmentSync { get; set; }
   }
}
