using ImageMagick.Drawing;

namespace RevoltImageGenApi.DataTransferObjects
{
    public class ServerImageRequestDTO
    {
        public string ServerID { get; set; }
        public string ServerName { get; set; }
        public string ImageUrl { get; set; }
        public string MemberCount { get; set; }
        public string Username { get; set; }

    }
}
