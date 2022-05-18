using Newtonsoft.Json;

namespace SimpleMessage
{
    //[System.Serializable]
    public class ProductMessage
    {
        public ProductMessage(string text)
        {
            Text = text;
        }

        [JsonProperty("t")]
        public string Text { get; private set; } // immutability 
    }
}
