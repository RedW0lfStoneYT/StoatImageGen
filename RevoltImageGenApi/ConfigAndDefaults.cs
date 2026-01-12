using System.Text.Json;

namespace RevoltImageGenApi
{
    public class ConfigAndDefaults
    {

        // Placeholders
        public static readonly string userNamePlaceholder = "{username}";
        public static readonly string serverNamePlaceholder = "{server_name}";
        public static readonly string serverMemberCountPlaceholder = "{total_members}";


        public static readonly byte[] defaultImageFileBytes;
        public static readonly string deafultWelcomeText;
        public static readonly string defaultTextColor;
        public static readonly string mongoConnectionString;
        public static readonly string mongoCollectionName;
        public static readonly string mongoAuthCollectionName;
        public static readonly string mongoDatabaseName;
        public static readonly string mongoEndpointLogCollection;
        public static readonly string mongoEndpointTelemetryCollection;

        // Image settings
        public static readonly (int x, uint width, uint height) profileImageLocation;
        public static readonly (int x, uint width, uint height) welcomeTextLocation;
        public static readonly string defaultFont;


        static ConfigAndDefaults()
        {
            // Read config.json
            var json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            userNamePlaceholder = root.GetProperty("userNamePlaceholder").GetString() ?? "{username}";
            serverNamePlaceholder = root.GetProperty("serverNamePlaceholder").GetString() ?? "{server_name}";
            serverMemberCountPlaceholder = root.GetProperty("serverMemberCountPlaceholder").GetString() ?? "{total_members}";

            defaultTextColor = root.GetProperty("defaultTextColor").GetString() ?? "#000000";
            deafultWelcomeText = root.GetProperty("defaultWelcomeText").GetString() ?? $"Welcome {userNamePlaceholder} to {serverNamePlaceholder},\nYou are user number {serverMemberCountPlaceholder}!";
            mongoConnectionString = root.GetProperty("mongo").GetProperty("connectionString").GetString() ?? "mongodb://localhost:27017/serverInfo";
            mongoCollectionName = root.GetProperty("mongo").GetProperty("collectionName").GetString() ?? "serverInfo";
            mongoAuthCollectionName = root.GetProperty("mongo").GetProperty("authCollectionName").GetString() ?? "authTokens";
            mongoEndpointLogCollection = root.GetProperty("mongo").GetProperty("endpointLogCollection").GetString() ?? "endpointLogs";
            mongoEndpointTelemetryCollection = root.GetProperty("mongo").GetProperty("endpointTelemetryCollection").GetString() ?? "endpointTelemetry";
            mongoDatabaseName = root.GetProperty("mongo").GetProperty("databaseName").GetString() ?? "serverInfo";

            var imagePath = root.GetProperty("defaultImageFilePath").GetString() ?? "DefaultBackground.png";
            defaultImageFileBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), imagePath));

            var profileLoc = root.GetProperty("imageSettings").GetProperty("profileImageLocation");
            profileImageLocation = (profileLoc.GetProperty("x").GetInt32(), profileLoc.GetProperty("width").GetUInt32(), profileLoc.GetProperty("height").GetUInt32());

            var welcomeLoc = root.GetProperty("imageSettings").GetProperty("welcomeTextLocation");
            welcomeTextLocation = (welcomeLoc.GetProperty("x").GetInt32(), welcomeLoc.GetProperty("width").GetUInt32(), welcomeLoc.GetProperty("height").GetUInt32());

            defaultFont = Path.Combine(Directory.GetCurrentDirectory(),
                root.GetProperty("imageSettings").GetProperty("defaultFontPath").GetString() ?? "DefaultFont/Helvetica.ttf");
        }
    }
}
