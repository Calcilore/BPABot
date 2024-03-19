namespace SocialCreditScoreBot2;

public interface ISentimentAnalyzer {
    public Task<bool> Init();
    public Task<double> Analyse(string text);
}