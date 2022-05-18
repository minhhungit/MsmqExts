using MsmqExts.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;

namespace MsmqExts
{
    public class DefaultSerializerHelper : ISerializerHelper
    {
        private readonly JsonSerializerSettings _serializerSettings = null;

        public DefaultSerializerHelper()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver()
            };
        }

        public object DeserializeObject(Stream stream, string messageTypeText, System.Text.Encoding encoding)
        {
            if (_serializerSettings == null)
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    return JsonConvert.DeserializeObject(reader.ReadToEnd(), Type.GetType(messageTypeText));
                }
            }
            else
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    return JsonConvert.DeserializeObject(reader.ReadToEnd(), Type.GetType(messageTypeText), _serializerSettings);
                }
            }
        }

        public string SerializeObject(object value)
        {
            if (_serializerSettings == null)
            {
                return JsonConvert.SerializeObject(value);
            }
            else
            {
                return JsonConvert.SerializeObject(value, settings: _serializerSettings);
            }            
        }
    }
}
