using Newtonsoft.Json;

namespace SocialCreditScoreBot2;

public static class ScoreManager {
    public static Dictionary<ulong, Score> Scores { get; private set; }
    private static Timer saveTimer;

    public static void Init() {
        if (!File.Exists("save.json")) {
            Scores = new Dictionary<ulong, Score>();
            Console.WriteLine("Creating New Save File");
            return;
        }
        
        string json = File.ReadAllText("save.json");
        Scores = JsonConvert.DeserializeObject<Dictionary<ulong, Score>>(json);
        Console.WriteLine("Loaded Save File");
        
        saveTimer = new Timer(_ => Save(), null, 300000, 300000);
    }

    public static void AddScore(ulong id, double amount, string text) {
        Score score;
        
        if (!Scores.ContainsKey(id)) {
            score = new Score();

            Scores[id] = score;
        }
        else {
            score = Scores[id];
            score.Total += amount;
        }
        
        if (amount < score.WorstScoreValue) {
            score.WorstScoreValue = amount;
            score.WorstScoreText = text;
        }
        
        if (amount > score.BestScoreValue) {
            score.BestScoreValue = amount;
            score.BestScoreText = text;
        }

        score.Sentences++;
    }

    public static Score GetScore(ulong id) {
        if (!Scores.ContainsKey(id)) {
            return new Score();
        }
        
        return Scores[id];
    }

    public static void Save() {
        Console.WriteLine("Saving...");
        File.WriteAllText("save.json", JsonConvert.SerializeObject(Scores));
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
