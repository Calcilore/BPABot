namespace SocialCreditScoreBot2;

public interface ISentimentAnalyzer {
    public Task<bool> Init();
    public Task<float> Analyse(string text);
}