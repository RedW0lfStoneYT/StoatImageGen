namespace RevoltImageGenApi.DataTransferObjects
{
    public class ServerImageDataPatchDto
    {
        public string ServerID { get; set; }     
        public string? Text { get; set; }        
        public string? TextColor { get; set; }   
        public IFormFile? Image { get; set; }    
        public IFormFile? CustomFont { get; set; }
        public string? Font { get; set; }
    }
}
