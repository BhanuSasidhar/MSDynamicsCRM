using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace proMX.Locobuzz.Plugins.JsonClass
{
   [DataContract]
   public class APIResponce
   {
      [DataMember(Name = "success")]
      public bool Success { get; set; }
      [DataMember(Name = "message")]
      public string Message { get; set; }
      [DataMember(Name = "data")]
      public object Data { get; set; }
      [DataMember(Name = "content")]
      public object Content { get; set; }
   }
}
