using System.Globalization;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json.Linq;

namespace SocialCreditScoreBot2;

internal static class Program {
    public static DiscordClient discord;
    
    public static async Task Main(string[] args) {
        // prevents numbers from being formatted with commas
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        
        // load config
        if (!File.Exists("config.json")) {
            File.WriteAllText("config.json", DefaultConfig);
            Console.WriteLine("Created config file");
        }
        
        string config = File.ReadAllText("config.json");
        JObject obj = JObject.Parse(config);
        
        string token = obj["token"]?.ToString();
        if (token is null or "ENTER TOKEN HERE") {
            Console.WriteLine("Please enter your bot token in the config file");
            return;
        }
        
        string model = obj["model"]?.ToString();
        if (model == null) {
            Console.WriteLine("Please enter your Vosk model in the config file");
            return;
        }
        
        Console.WriteLine("Successfully loaded config");
        
        ScoreManager.Init();
        
        discord = new DiscordClient(new DiscordConfiguration() {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.DirectMessages
        });

        SlashCommandsExtension commands = discord.UseSlashCommands();
        
        await commands.RefreshCommands();
        commands.RegisterCommands<Commands>();
        Console.WriteLine("Registered Commands");

        discord.MessageCreated += Commands.MessageHandler;
        
        discord.UseVoiceNext(new VoiceNextConfiguration(){
            EnableIncoming = true
        });
        Console.WriteLine("Registered VoiceNext");
        
        SentimentAnalyser.Init(model);

        await discord.ConnectAsync();
        Console.WriteLine("Started Bot");
        
        await Task.Delay(-1);
    }

    private const string DefaultConfig = "{\n    \"token\": \"ENTER TOKEN HERE\",\n    \"model\": \"vosk-model-en-us-0.22\"\n}";
}
