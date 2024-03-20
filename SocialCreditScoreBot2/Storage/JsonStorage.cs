using Newtonsoft.Json;
using SocialCreditScoreBot2.Interfaces;

namespace SocialCreditScoreBot2.Storage;

public class JsonStorage : IStorageMethod {
    private Dictionary<ulong, Score> scores;
    private Timer saveTimer;
    
    public async Task<bool> Init() {
        if (!File.Exists("save.json")) {
            scores = new Dictionary<ulong, Score>();
            Console.WriteLine("Creating New Save File");
            return true;
        }
        
        string json = await File.ReadAllTextAsync("save.json");
        scores = JsonConvert.DeserializeObject<Dictionary<ulong, Score>>(json)!;
        Console.WriteLine("Loaded Save File");
        
        saveTimer = new Timer(_ => Save(), null, 300000, 300000);
        return true;
    }

    public async Task Close() {
        Save();
        await saveTimer.DisposeAsync();
        scores = null!;
    }

    private void Save() {
        Console.WriteLine("Saving json data...");
        File.WriteAllText("save.json", JsonConvert.SerializeObject(scores));
    }

    public Task SetScore(ulong id, Score score) {
        scores[id] = score;
        return Task.CompletedTask;
    }

    public Task<Score> GetScore(ulong id) {
        if (!scores.TryGetValue(id, out Score? value)) {
            return Task.FromResult(new Score());
        }
        
        return Task.FromResult(value);
    }

    public Task<Dictionary<ulong, Score>> GetUsersScores(ulong[] users) {
        Dictionary<ulong, Score> result = new();
        
        foreach (ulong user in users) {
            if (scores.TryGetValue(user, out Score? value)) {
                result.Add(user, value);
            }
        }
        
        return Task.FromResult(result);
    }
}