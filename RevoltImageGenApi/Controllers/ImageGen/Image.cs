using ImageMagick;
using ImageMagick.Drawing;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using RevoltImageGenApi.DataTransferObjects;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace RevoltImageGenApi.Controllers.ImageGen
{
    [Route("api/[controller]")]
    [ApiController]
    public class Image : ControllerBase
    {
        [HttpPost]
        [Route("welcome")]
        public async Task<IActionResult> getImage([FromHeader] string authToken, [FromForm] ServerImageRequestDTO request)
        {
            if (!Utils.authenticate(authToken))
                return Unauthorized("You do not have access to this endpoint");
            if (request.ServerID == null)
                return BadRequest("Server ID must not be null");
            (ServerImageData server, bool isNew) serverRequest = Utils.getData<ServerImageData>(request.ServerID);
            ServerImageData server = serverRequest.server;
            if (server == null)
                return NotFound("There was an error finding/generating the server data");
            using MagickImage image = new MagickImage(server.Image ?? ConfigAndDefaults.defaultImageFileBytes);

            ComposeWelcomeText(image, request, server);
            await ComposeProfilePicture(image, request.ImageUrl);

            if (serverRequest.isNew)
                await Utils.getCollection<ServerImageData>(ConfigAndDefaults.mongoCollectionName).InsertOneAsync(serverRequest.server);

            return File(image.ToByteArray(), "image/png", $"{server.ServerID}-{request.Username}-welcome.png");
        }

        [HttpPatch]
        [Route("welcome")]
        public async Task<IActionResult> setWelcome([FromForm] ServerImageDataPatchDto patch, [FromHeader] string authToken)
        {
            if (!Utils.authenticate(authToken)) 
                return Unauthorized("You do not have access to this endpoint");
            (ServerImageData data, bool isNew) result = await Utils.getDataAsync<ServerImageData>(patch.ServerID);
            ServerImageData existing = result.data;
            if (existing == null) return BadRequest("Could not get data");

            if (!string.IsNullOrEmpty(patch.Text))
                existing.Text = patch.Text;

            if (!string.IsNullOrEmpty(patch.TextColor))
                existing.Color = patch.TextColor;

            if (patch.Image != null)
            {
                using var ms = new MemoryStream();
                await patch.Image.CopyToAsync(ms);
                existing.Image = ms.ToArray();
            }
            if (patch.CustomFont != null)
            {
                using var ms = new MemoryStream();
                await patch.CustomFont.CopyToAsync(ms);
                existing.CustomFont = ms.ToArray();
                existing.Font = null;
            }
            if (!string.IsNullOrEmpty(patch.Font))
            {
                existing.Font = patch.Font;
                existing.CustomFont = null;
            }

            if (result.isNew)
                await Utils.getCollection<ServerImageData>(ConfigAndDefaults.mongoCollectionName).InsertOneAsync(existing);
            else
                await Utils.getCollection<ServerImageData>(ConfigAndDefaults.mongoCollectionName).ReplaceOneAsync(Utils.getFilter<ServerImageData>(patch.ServerID), existing);

            return Ok("Server details have been updated!");
        }

        [HttpPost]
        [Route("config/full")]
        public IActionResult GetRawWelcome([FromHeader] string authToken, string ServerID)
        {
            if (!Utils.authenticate(authToken))
                return Unauthorized("You do not have access to this endpoint");
            if (ServerID == null)
                return BadRequest("Server ID must not be null");
            (ServerImageData server, bool isNew) serverRequest = Utils.getData<ServerImageData>(ServerID);
            ServerImageData server = serverRequest.server;
            return Ok(new ServerWelcomeRawDTO
            {
                ServerID = server.ServerID,
                Color = server.Color,
                Text = server.Text,
                Font = server.Font,
                HasCustomFont = server.CustomFont?.Length > 0,
                HasImage = server.Image?.Length > 0
            });
        }

        [HttpPost]
        [Route("config/image")]
        public IActionResult GetWelcomeBackground([FromHeader] string authToken, string ServerID)
        {
            if (!Utils.authenticate(authToken))
                return Unauthorized("You do not have access to this endpoint");
            if (ServerID == null)
                return BadRequest("Server ID must not be null");
            (ServerImageData server, bool isNew) serverRequest = Utils.getData<ServerImageData>(ServerID);
            ServerImageData server = serverRequest.server;
            return File(server.Image ?? ConfigAndDefaults.defaultImageFileBytes, "image/png", $"{ServerID}-welcome.png");
        }

        [HttpPost]
        [Route("config/customfont")]
        public IActionResult GetWelcomeCustomFont([FromHeader] string authToken, string ServerID)
        {
            if (!Utils.authenticate(authToken))
                return Unauthorized("You do not have access to this endpoint");
            if (ServerID == null)
                return BadRequest("Server ID must not be null");
            (ServerImageData server, bool isNew) serverRequest = Utils.getData<ServerImageData>(ServerID);
            ServerImageData server = serverRequest.server;
            if (server.CustomFont != null)
                return File(server.CustomFont, "font/ttf", $"{ServerID}-font.ttf");
            return NotFound("Server does not have a custom font selected");
        }

        [HttpGet]
        [Route("fonts")]
        public IActionResult GetAllFonts()
        {
            return Ok(new { fonts = MagickNET.FontNames });
        }

        [HttpDelete]
        [Route("welcome")]
        public async Task<IActionResult> DeleteServer([FromHeader] string authToken, string ServerID)
        {
            if (!Utils.authenticate(authToken))
                return Unauthorized("You do not have access to this endpoint");
            if (ServerID == null)
                return BadRequest("Server ID must not be null");
            (ServerImageData server, bool isNew) serverRequest = Utils.getData<ServerImageData>(ServerID);
            ServerImageData server = serverRequest.server;
            if (string.IsNullOrEmpty(server.ServerID))
                return NotFound("There is no server with that ID");
            if (serverRequest.isNew)
                return NotFound("Server does not exist");
            var filter = Utils.getFilter<BsonDocument>(server.ServerID);
            DeleteResult deleted = await Utils.getCollection<BsonDocument>(ConfigAndDefaults.mongoCollectionName).DeleteOneAsync(filter);
            return deleted.IsAcknowledged ? Ok($"Deleted server {ServerID}") : Problem($"There was an error deleting server with ID {ServerID}: {deleted}");
        }

        private async Task<Stream> GetImageStreamAsync(string url)
        {
            using var httpClient = new HttpClient();
            // This will return a stream directly from the response body
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Copy the stream into memory if you need a seekable stream
            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // reset position before returning
            return memoryStream;
        }

        private MagickImage CircleCropImage(MagickImage image)
        {
            using var mask = new MagickImage(MagickColors.Black, image.Width, image.Height);
            mask.Draw(
                new DrawableFillColor(MagickColors.White),
                new DrawableCircle(image.Width/2, image.Height/2, image.Width/2, 1)
            );
            mask.Alpha(AlphaOption.Copy);

            image.Composite(mask, CompositeOperator.CopyAlpha);
            return image;
        }

        private MagickImage ComposeWelcomeText(MagickImage image, ServerImageRequestDTO request, ServerImageData server)
        {
            var textLocation = ConfigAndDefaults.welcomeTextLocation;
            string font = GetFont(server);

            var settings = new MagickReadSettings
            {
                Font = font,
                TextGravity = Gravity.West,
                BackgroundColor = MagickColors.Transparent,
                FillColor = new MagickColor(server.Color),
                FontPointsize = 24,
                Height = textLocation.height,
                Width = textLocation.width
            };
            var caption = new MagickImage($"caption:{ReplacePlaceholders(server.Text, request.ServerName, request.Username, request.MemberCount)}", settings);
            int y = (int)(image.Height - caption.Height) / 2;
            image.Composite(caption, textLocation.x, y, CompositeOperator.Over);
            if (font.Contains(server.ServerID))
                System.IO.File.Delete(font);
            return image;
        }

        private string ReplacePlaceholders(string welcomeText, string serverName, string username, string memberCount)
        {
            return welcomeText
                .Replace(ConfigAndDefaults.serverNamePlaceholder, serverName)
                .Replace(ConfigAndDefaults.userNamePlaceholder, username)
                .Replace(ConfigAndDefaults.serverMemberCountPlaceholder, memberCount);
        }

        private async Task<MagickImage> ComposeProfilePicture(MagickImage image, string imageUrl)
        {
            using var imageStream = await GetImageStreamAsync(imageUrl);
            using MagickImage profileImage = CircleCropImage(new MagickImage(imageStream));
            var profileLocation = ConfigAndDefaults.profileImageLocation;
            profileImage.Scale(profileLocation.width, profileLocation.height);
            int y = (int)(image.Height - profileImage.Height) / 2;
            image.Composite(profileImage, profileLocation.x, y, CompositeOperator.Over);

            return image;
        }

        private string GetFont(ServerImageData server)
        {
            if (server.CustomFont == null || server.CustomFont.Length == 0)
            {
                if (string.IsNullOrEmpty(server.Font))
                    return ConfigAndDefaults.defaultFont;
                return ValidateFont(server.Font) ? server.Font : ConfigAndDefaults.defaultFont;
            }
            string tempPath = Path.Combine(Path.GetTempPath(), $"{server.ServerID}_font.ttf");
            System.IO.File.WriteAllBytes(tempPath, server.CustomFont);
            return tempPath;
        }


        private bool ValidateFont(string fontName)
        {
            return MagickNET.FontNames.Contains(fontName);
        }
    }
}
