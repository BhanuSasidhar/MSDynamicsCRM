using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   class EntityMappingConfigurationJsonMapping
   {
      public List<JSONFieldMapping> Fields { get; set; }
      public string EntityMappingID { get; set; }

   }
   public class JSONFieldMapping
   {
      public string CRMUserID { get; set; }
      public string CRMUserName { get; set; }
      public string LocoBuzzUserName { get; set; }
      public int LocoBuzzUserId { get; set; }
   }
}
