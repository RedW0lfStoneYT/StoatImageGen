namespace RevoltImageGenApi.DataTransferObjects
{
    public class ServerWelcomeRawDTO
    {
        public string ServerID { get; set; }
        public string Color { get; set; }
        public string Text { get; set; }
        public string? Font { get; set; }
        public bool HasImage { get; set; }
        public bool HasCustomFont { get; set; }
    }
}
