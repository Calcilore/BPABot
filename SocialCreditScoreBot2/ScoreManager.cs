using System.Diagnostics;
using SocialCreditScoreBot2.Interfaces;
using SocialCreditScoreBot2.Storage;

namespace SocialCreditScoreBot2;

public static class ScoreManager {
    private static IStorageMethod? storage;

    public static async Task<bool> Init() {
        storage = Program.Config.StorageMethod switch {
            "json" => new JsonStorage(),
            "sqlite" => new SqliteStorage(),
            _ => null
        };

        if (storage == null) {
            Console.WriteLine("Invalid StorageMethod in config, valid options are: json, sqlite");
            return false;
        }
        
        return await storage.Init();
    }

    public static async Task AddScore(ulong id, double amount, string text) {
        Debug.Assert(storage != null);
        Score score = await storage.GetScore(id);
        
        score.Total += amount;
        
        if (amount < score.WorstScoreValue) {
            score.WorstScoreValue = amount;
            score.WorstScoreText = text;
        }
        
        if (amount > score.BestScoreValue) {
            score.BestScoreValue = amount;
            score.BestScoreText = text;
        }

        score.Sentences++;
        
        // Save score
        await storage.SetScore(id, score);
    }

    public static Task<Score> GetScore(ulong id) {
        Debug.Assert(storage != null);
        return storage.GetScore(id);
    }
    
    public static Task<Dictionary<ulong, Score>> GetUsersScores(ulong[] users) {
        Debug.Assert(storage != null);
        return storage.GetUsersScores(users);
    }

    public static void Close() {
        storage?.Close();
    }
}

public class Score {
    public double Total = 0.0f;
    public uint Sentences = 0;
    public string WorstScoreText = "";
    public double WorstScoreValue = 1000000f;
    public string BestScoreText = "";
    public double BestScoreValue = -1000000f;
    
    public double GetBpa() {
        return (Total / Sentences) * 2f + 3f;
    }
    
    public double GetTotal() {
        return Total * 20f;
    }
    
    public double GetBest() {
        return BestScoreValue * 2f + 3f;
    }
    
    public double GetWorst() {
        return WorstScoreValue * 2f + 3f;
    }
}
