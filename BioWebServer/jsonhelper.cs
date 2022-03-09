using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ServiceModel.Web;
using System.ServiceModel;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace BioWebServer
{
    public static class  jsonhelper
    {
        public static T parse<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }

        public static string stringify(object jsonObject)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
