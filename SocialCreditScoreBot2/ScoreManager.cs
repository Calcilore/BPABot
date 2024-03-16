using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SocialCreditScoreBot2;

public class ScoreManager {
    private static Dictionary<ulong, Score> scores;

    public static void Init() {
        if (!File.Exists("save.json")) {
            scores = new Dictionary<ulong, Score>();
            Console.WriteLine("Creating New Save File");
            return;
        }
        
        string json = File.ReadAllText("save.json");
        scores = JsonConvert.DeserializeObject<Dictionary<ulong, Score>>(json);
        Console.WriteLine("Loaded Save File");
    }

    public static void AddScore(ulong id, float amount, string text) {
        Score score;
        
        if (!scores.ContainsKey(id)) {
            score = new Score();

            scores[id] = score;
        }
        else {
            score = scores[id];
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
        
        File.WriteAllText("save.json", JsonConvert.SerializeObject(scores));
    }

    public static Score GetScore(ulong id) {
        if (!scores.ContainsKey(id)) {
            return new Score();
        }
        
        return scores[id];
    }
}

public class Score {
    public float Total = 0.0f;
    public uint Sentences = 0;
    public string WorstScoreText = "";
    public float WorstScoreValue = 1000000f;
    public string BestScoreText = "";
    public float BestScoreValue = -1000000f;
}
