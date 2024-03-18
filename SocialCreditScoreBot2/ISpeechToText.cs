namespace SocialCreditScoreBot2;

public interface ISpeechToText {
    public Task<bool> Init(string modelPath);
    public Task<string> Synthesize(byte[] data);
}