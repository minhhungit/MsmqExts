using System;
using System.IO;
using Newtonsoft.Json;

namespace MsmqExts.Extensions
{
    public static class StreamExtensions
    {
        public static string ReadToEnd(this Stream stream)
        {
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        //public static T ReadFromJson<T>(this Stream stream)
        //{
        //    var json = stream.ReadToEnd();
        //    return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
        //    {
        //        ContractResolver = new PrivateSetterContractResolver()
        //    });
        //}

        public static object ReadFromJson(this Stream stream, string messageType)
        {
            //return JsonConvert.DeserializeObject(stream.ReadToEnd(), Type.GetType(messageType));

            return JsonConvert.DeserializeObject(stream.ReadToEnd(), Type.GetType(messageType), new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver()
            });
        }
    }
}
