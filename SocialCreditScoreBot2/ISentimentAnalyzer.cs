namespace SocialCreditScoreBot2;

public interface ISentimentAnalyzer {
    public void Init();
    public float Analyse(string text);
}