using System.Text.Json;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;

namespace SocialCreditScoreBot2;

public class Commands : ApplicationCommandModule {
    private static Dictionary<ulong, List<byte>> speakData = new();
    private static Dictionary<ulong, bool> speakWait = new();

    private const int SampleLength = 10; // seconds
    
    [SlashCommand("join", "Joins the current voice channel.")]
    public async Task JoinCommand(InteractionContext ctx) {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        DiscordMember member = ctx.Member;
        if (member.VoiceState == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("You are not in a voice channel")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        DiscordChannel channel = member.VoiceState.Channel;

        Console.WriteLine("Joining voice channel");
        VoiceNextConnection? connection = await channel.ConnectAsync();
        connection.VoiceReceived += ReceiveHandler;
        Console.WriteLine("Joined voice channel");
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Joined Voice Channel")
                .WithColor(DiscordColor.Green)));
    }
    
    [SlashCommand("leave", "Leaves the current voice channel.")]
    public async Task LeaveCommand(InteractionContext ctx) {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        VoiceNextExtension? vnext = ctx.Client.GetVoiceNext();

        VoiceNextConnection? connection = vnext.GetConnection(ctx.Guild);
        if (connection == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("There is no voice connection to leave")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        connection.VoiceReceived -= ReceiveHandler;
        connection.Dispose();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Left Voice Channel")
                .WithColor(DiscordColor.Green)));
    }

    [SlashCommand("score", "Gets the current social credit score of the user.")]
    public async Task ScoreCommand(InteractionContext ctx, [Option("user", "The user to get the score of.")] DiscordUser? user = null) {
        if (user == null) {
            user = ctx.User;
        }
        
        Score score = ScoreManager.GetScore(user.Id);

        if (score.Sentences == 0) {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("No sentences have been analysed yet."));
            return;
        }
        
        DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
        string username = member.Mention;
        
        // BPA is the average sentiment, and it has a range of 1 to 5
        float average = (score.Total / score.Sentences) * 2f + 3f;
        DiscordColor color = average > 2.8 ? DiscordColor.Green : 
            average < 2.2 ? DiscordColor.Red : DiscordColor.Yellow;
        
        string message = $"{username}'s BPA is **{average:F2}**.\n";
        message += $"{username} has a total of **{(score.Total*20f):F2}** Behavior Points.";
        if (score.BestScoreText != "") {
            message += $"\n\nBest Sentence: **{(score.BestScoreValue * 2f + 3f):F2}**: **\"{score.BestScoreText }\"**" + 
                       $"\nWorst Sentence: **{(score.WorstScoreValue * 2f + 3f):F2}**: **\"{score.WorstScoreText}\"**";
        }
        
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("BPA Score")
                .WithDescription(message)
                .WithColor(color)));
    }
    
    
    public static async Task MessageHandler(DiscordClient _, DSharpPlus.EventArgs.MessageCreateEventArgs e) {
        string text = e.Message.Content;
        float sentiment = SentimentAnalyser.Analyse(text);
        
        ScoreManager.AddScore(e.Author.Id, sentiment/8f, text);
        
        Console.WriteLine(text + ": " + sentiment);
    }

    private async Task ReceiveHandler(VoiceNextConnection _, VoiceReceiveEventArgs args) {
        if (args.User == null) return;

        ulong id = args.User.Id;

        speakWait.TryAdd(id, false);
        
        while (speakWait[id]) {
            await Task.Delay(5);
        }
            
        if (!speakData.ContainsKey(id)) {
            speakData[id] = new List<byte>();
        }

        speakWait[id] = true;
        
        speakData[id].AddRange(args.PcmData.Span);

        // only save to file if we have enough data
        // Console.WriteLine(speakData[id].Count / args.AudioFormat.SampleRate + " " + args.AudioFormat.SampleRate);
        if (speakData[id].Count <= SampleLength * args.AudioFormat.SampleRate) {
            speakWait[id] = false;
            return;
        }

        string name = $"{id}_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.wav";
        byte[] data = speakData[id].ToArray();
        speakData[id] = new List<byte>();
        speakWait[id] = false;
        
        // this is after wait is set to false so that the next speaking event can be handled while this is being processed

        Dictionary<string, JsonElement> speech = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SentimentAnalyser.Synthesize(data));
        string text = speech["text"].GetString();

        float sentiment = SentimentAnalyser.Analyse(text);
        
        ScoreManager.AddScore(id, sentiment, text);
        
        Console.WriteLine(text + ": " + sentiment);

        // Process ffmpeg = Process.Start(new ProcessStartInfo {
        //     FileName = "ffmpeg",
        //     Arguments = $@"-ac 2 -f s16le -ar {args.AudioFormat.SampleRate / 2} -i pipe:0 -ac 2 -ar 44100 out/{name}",
        //     RedirectStandardInput = true
        // })!;
        //
        // await ffmpeg.StandardInput.BaseStream.WriteAsync(data);
        // await ffmpeg.WaitForExitAsync();
        // ffmpeg.Dispose();
    }
}