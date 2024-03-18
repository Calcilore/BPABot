using VaderSharp2;
using Vosk;

namespace SocialCreditScoreBot2.Implementations;

public class VaderSharp : ISentimentAnalyzer {
    public Task<bool> Init() {return Task.FromResult(true);}

    public Task<float> Analyse(string text) {
        SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
        
        SentimentAnalysisResults results = analyzer.PolarityScores(text);
        
        return Task.FromResult((float)results.Compound);
    }
}