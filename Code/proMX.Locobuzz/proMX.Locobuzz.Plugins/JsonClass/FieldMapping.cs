using System.Collections.Generic;
using System.Runtime.Serialization;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   [DataContract]
   public class FieldMapping
   {
      [DataMember]
      public List<Option> Option { get; set; }
   }

   [DataContract]
   public class Option
   {
      [DataMember]
      public List<OptionValue> OptionValue { get; set; }
   }

   [DataContract]
   public class OptionValue
   {
      [DataMember]
      public string CRMOptionSetName { get; set; }
      [DataMember]
      public int CRMOptionSetValue { get; set; }
      [DataMember]
      public int CRMOptionSetParent { get; set; }
      [DataMember]
      public string CRMOptionSetParentName { get; set; }
      [DataMember]
      public string LocobuzzOptionSetName { get; set; }
      [DataMember]
      public int LocobuzzOptionSetValue { get; set; }
   }
}
