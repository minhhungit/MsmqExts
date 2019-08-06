using Newtonsoft.Json;

namespace DemoMsmqExts.Messages
{
    public class Product
    {
        [JsonProperty(PropertyName = "i")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }
    }
}
