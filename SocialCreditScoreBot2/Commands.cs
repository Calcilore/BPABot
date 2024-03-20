using System.Text;
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
        if (member == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("You cannot use this command in a DM")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        if (member.VoiceState == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("You are not in a voice channel")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        DiscordChannel channel = member.VoiceState.Channel;

        Logging.Debug("Joining voice channel");
        VoiceNextConnection? connection = await channel.ConnectAsync();
        connection.VoiceReceived += ReceiveHandler;
        Logging.Debug("Joined voice channel");
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Joined Voice Channel")
                .WithColor(DiscordColor.Green)));
    }
    
    [SlashCommand("leave", "Leaves the current voice channel.")]
    public async Task LeaveCommand(InteractionContext ctx) {
        if (ctx.Guild == null) {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("You cannot use this command in a DM")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        if (!LeaveVoiceChannel(ctx.Client, ctx.Guild)) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("There is no voice connection to leave")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Left Voice Channel")
                .WithColor(DiscordColor.Green)));
    }
    
    public static bool LeaveVoiceChannel(DiscordClient client, DiscordGuild guild) {
        VoiceNextExtension? vnext = client.GetVoiceNext();

        VoiceNextConnection? connection = vnext.GetConnection(guild);
        if (connection == null) {
            return false;
        }
        
        connection.VoiceReceived -= ReceiveHandler;
        connection.Dispose();

        return true;
    }

    [SlashCommand("score", "Gets the current social credit score of the user.")]
    public async Task ScoreCommand(InteractionContext ctx, [Option("User", "The user to get the score of.")] DiscordUser? user = null) {
        if (user == null) {
            user = ctx.User;
        }
        
        Score score = await ScoreManager.GetScore(user.Id);

        if (score.Sentences == 0) {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("No sentences have been analysed yet.")
                .WithColor(DiscordColor.Red)));
            return;
        }
        
        string username = user.Mention;
        
        // BPA is the average sentiment, and it has a range of 1 to 5
        double average = score.GetBpa();
        DiscordColor color = average > 3.2 ? DiscordColor.Green : 
            average < 2.8 ? DiscordColor.Red : DiscordColor.Yellow;
        
        string message = $"{username}'s BPA is **{average:F2}**.\n";
        message += $"{username} has a total of **{(score.GetTotal()):F3}** Behavior Points.";
        if (score.BestScoreText != "") {
            message += $"\n\nBest Sentence: **{(score.GetBest()):F3}**: **\"{score.BestScoreText }\"**" + 
                       $"\nWorst Sentence: **{(score.GetWorst()):F3}**: **\"{score.WorstScoreText}\"**";
        }
        
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("BPA Score")
                .WithDescription(message)
                .WithColor(color)));
    }
    
    [SlashCommand("leaderboard", "Gets the top 12 users with the highest social credit score.")]
    public async Task LeaderboardCommand(InteractionContext ctx, [
        Option("SortMode", "Whether to sort by total or average")] SortMode sort = SortMode.Average, 
        [Option("SortOrder", "Whether to sort by best or worst")] SortOrder order = SortOrder.BestFirst) {
        
        if (ctx.Guild == null) {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("You cannot use this command in a DM")
                    .WithColor(DiscordColor.Red)));
            return;
        }
        
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        ulong[] members = (await ctx.Guild.GetAllMembersAsync()).Select(v => v.Id).ToArray();
        List<KeyValuePair<ulong, Score>> scores = (await ScoreManager.GetUsersScores(members)).ToList();  // List because we need .Sort()
        
        // sort scores, in best first order
        scores.Sort((a, b) => {
            double aScore;
            double bScore;

            switch (sort) {
                case SortMode.Average:
                    aScore = a.Value.Total / a.Value.Sentences;
                    bScore = b.Value.Total / b.Value.Sentences;
                    break;

                case SortMode.Total:
                    aScore = a.Value.Total;
                    bScore = b.Value.Total;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(sort), sort, null);
            }

            return bScore.CompareTo(aScore);
        });
        
        // get the range of scores to display
        IEnumerable<int> range = order switch {
            SortOrder.BestFirst => Enumerable.Range(0, Math.Min(12, scores.Count)),
            SortOrder.WorstFirst => Enumerable.Range(Math.Max(0, scores.Count-12), scores.Count).Reverse(),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
        };
        
        StringBuilder message = new StringBuilder();
        foreach (int i in range) {
            DiscordUser member = await ctx.Client.GetUserAsync(scores[i].Key);
            
            if (order == SortOrder.BestFirst)
                message.Append($"{i+1}. {member.Mention} has a BPA of **{(scores[i].Value.GetBpa()):F2}** and a total of **{(scores[i].Value.GetTotal()):F3}**\n");
            else
                message.Append($"{i+1}\\. {member.Mention} has a BPA of **{(scores[i].Value.GetBpa()):F2}** and a total of **{(scores[i].Value.GetTotal()):F3}**\n");
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Leaderboard")
                .WithDescription(message.ToString())
                .WithColor(DiscordColor.Gold)));
    }
    
    public static async Task MessageHandler(DiscordClient _, DSharpPlus.EventArgs.MessageCreateEventArgs e) {
        if (e.Author.IsBot && Program.Config.IgnoreBots) return;
        
        string text = e.Message.Content;
        double sentiment = await Program.SentimentAnalyser.Analyse(text);
        
        await ScoreManager.AddScore(e.Author.Id, sentiment/8d, text);
        
        Logging.Verbose(text + ": " + sentiment);
    }

    private static async Task ReceiveHandler(VoiceNextConnection _, VoiceReceiveEventArgs args) {
        if (args.User == null || (args.User.IsBot && Program.Config.IgnoreBots)) return;

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
        if (speakData[id].Count <= SampleLength * args.AudioFormat.SampleRate) {
            speakWait[id] = false;
            return;
        }

        byte[] data = speakData[id].ToArray(); // this data is in 16 bit little endian PCM format
        speakData[id] = new List<byte>();
        speakWait[id] = false;
        
        // this is after wait is set to false so that the next speaking event can be handled while this is being processed

        string text = await Program.SpeechToText.Synthesize(data);
        double sentiment = await Program.SentimentAnalyser.Analyse(text);
        
        await ScoreManager.AddScore(id, sentiment, text);
        
        Logging.Verbose(args.User.Username + ": " + text + " - " + sentiment);
    }
}

public enum SortMode {
    [ChoiceName("Average")]
    Average,
    [ChoiceName("Total")]
    Total,
}

public enum SortOrder {
    [ChoiceName("Best First")]
    BestFirst,
    [ChoiceName("Worst First")]
    WorstFirst,
}
