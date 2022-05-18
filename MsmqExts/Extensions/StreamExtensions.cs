//using System;
//using System.IO;
//using Newtonsoft.Json;

//namespace MsmqExts.Extensions
//{
//    public static class StreamExtensions
//    {
//        //public static string ReadToEnd(this Stream stream)
//        //{
//        //    var reader = new StreamReader(stream);
//        //    return reader.ReadToEnd();
//        //}

//        //public static T ReadFromJson<T>(this Stream stream, JsonSerializerSettings jsonSerializerSettings)
//        //{
//        //    if (jsonSerializerSettings == null)
//        //    {
//        //        return JsonConvert.DeserializeObject<T>(stream.ReadToEnd());
//        //    }
//        //    else
//        //    {
//        //        return JsonConvert.DeserializeObject<T>(stream.ReadToEnd(), jsonSerializerSettings);
//        //    }
//        //}

//        public static object ReadFromJson(this Stream stream, string messageType, JsonSerializerSettings jsonSerializerSettings)
//        {
//            //return JsonConvert.DeserializeObject(stream.ReadToEnd(), Type.GetType(messageType));

//            if (jsonSerializerSettings == null)
//            {
//                using (var reader = new StreamReader(stream))
//                {
//                    return JsonConvert.DeserializeObject(reader.ReadToEnd(), Type.GetType(messageType));
//                }
//            }
//            else
//            {
//                using (var reader = new StreamReader(stream))
//                {
//                    return JsonConvert.DeserializeObject(reader.ReadToEnd(), Type.GetType(messageType), jsonSerializerSettings);
//                }
//            }
//        }
//    }
//}
