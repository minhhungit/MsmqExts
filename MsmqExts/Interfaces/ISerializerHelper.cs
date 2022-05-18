using System.IO;

namespace MsmqExts
{
    public interface ISerializerHelper
    {
        string SerializeObject(object value);
        object DeserializeObject(Stream stream, string messageTypeText, System.Text.Encoding encoding);
    }
}
