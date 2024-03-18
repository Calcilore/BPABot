namespace SocialCreditScoreBot2;

public interface ISpeechToText {
    public void Init(string modelPath);
    public string Synthesize(byte[] data);
}