using System.Globalization;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;

namespace SocialCreditScoreBot2;

internal static class Program {
    private static DiscordClient discord;
    public static ISpeechToText SpeechToText;
    public static ISentimentAnalyzer SentimentAnalyser;
    public static Config Config;
    
    public static async Task Main(string[] args) {
        // prevents numbers from being formatted with commas
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        
        // load config
        if (!File.Exists("config.json")) {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(new Config(), Formatting.Indented));
            Console.WriteLine("Created config file");
        }
        
        Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"))!;

        if (Config.Token is null or "ENTER TOKEN HERE") {
            Console.WriteLine("Please enter your bot token in the config file");
            return;
        }

        if (Config.Model == null) {
            Console.WriteLine("Please enter your Speech Analyser model in the config file");
            return;
        }

        switch (Config.SpeechToTextLibrary) {
            case "vosk":
                SpeechToText = new Implementations.Vosk();
                break;
            
            case "whisper":
                SpeechToText = new Implementations.Whisper();
                break;
            
            default:
                Console.WriteLine("Invalid SpeechToTextLibrary in config, valid options are: vosk, whisper");
                return;
        }

        switch (Config.SentimentAnalyzerLibrary) {
            case "vader":
                SentimentAnalyser = new Implementations.VaderSharp();
                break;
            
            default:
                Console.WriteLine("Invalid SentimentAnalyzerLibrary in config, valid options are: vader");
                return;
        }
        
        Console.WriteLine("Successfully loaded config");
        
        ScoreManager.Init();
        
        discord = new DiscordClient(new DiscordConfiguration() {
            Token = Config.Token,
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

        if (!await SentimentAnalyser.Init()) {
            Console.WriteLine("Failed to start SentimentAnalyser");
            return;
        }
        Console.WriteLine("Registered SentimentAnalyser");
        
        if (!await SpeechToText.Init(Config.Model)) {
            Console.WriteLine("Failed to start SpeechToText");
            return;
        }
        Console.WriteLine("Registered SpeechToText");

        await discord.ConnectAsync();
        Console.WriteLine("Started Bot");

        Console.CancelKeyPress += OnExit;
        
        await Task.Delay(-1);
    }
    
    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        Console.WriteLine("Exiting...");
        ScoreManager.Save();

        // leave all voice channels
        foreach (DiscordGuild guild in discord.Guilds.Values) {
            Commands.LeaveVoiceChannel(discord, guild);
        }

        e.Cancel = true;
        Environment.Exit(0);
    }
}
