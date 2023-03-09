using System.Collections.Generic;
using System.Runtime.Serialization;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   [DataContract()]
   public partial class ContactFieldsDetails
   {
      [DataMember()]
      public List<Field> Fields { get; set; }
   }

   [DataContract()]
   public class Field
   {
      [DataMember()]
      public string CRMLogicalName { get; set; }

      [DataMember()]
      public string CRMSchemaName { get; set; }

      [DataMember()]
      public string CRMDataType { get; set; }

      [DataMember()]
      public string CRMDisplayName { get; set; }

      [DataMember()]
      public string LocobuzzField { get; set; }

      [DataMember()]
      public string LocobuzzDataType { get; set; }

      [DataMember()]
      public string CRMLookupType { get; set; }
   }
}
