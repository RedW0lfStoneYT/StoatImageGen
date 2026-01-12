using MongoDB.Bson.Serialization.Attributes;
using System.Drawing;
using System.Runtime.Serialization;

namespace RevoltImageGenApi.Controllers.ImageGen
{
    [BsonIgnoreExtraElements]
    public class ServerImageData : IServerData
    {
        [DataMember]
        [BsonElement("SERVERID")]
        public string ServerID { get; set; } = "";

        [DataMember]
        [BsonElement("TEXT_COLOR")]
        public string Color { get; set; } = ConfigAndDefaults.defaultTextColor;

        [DataMember]
        [BsonElement("BACKGROUND_IMAGE")]
        public byte[]? Image { get; set; }

        [DataMember]
        [BsonElement("WELCOME_TEXT")]
        public string Text { get; set; } = ConfigAndDefaults.deafultWelcomeText;
        [DataMember]
        [BsonElement("WELCOME_FONT")]
        public byte[]? CustomFont { get; set; }
        [DataMember]
        [BsonElement("FONT_NAME")]
        public string? Font {  get; set; }  

    }
}
