using VaderSharp2;
using Vosk;

namespace SocialCreditScoreBot2.Implementations;

public class VaderSharp : ISentimentAnalyzer {
    public void Init() {}

    public float Analyse(string text) {
        SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
        
        SentimentAnalysisResults results = analyzer.PolarityScores(text);

        return (float)results.Compound;
    }
}