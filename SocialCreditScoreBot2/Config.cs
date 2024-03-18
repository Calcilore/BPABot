namespace SocialCreditScoreBot2;

public class Config {
    public string Token = "ENTER TOKEN HERE";
    public string SpeechToTextLibrary = "whisper";
    public string SentimentAnalyzerLibrary = "vader";
    public string Model = "models/ggml-baseEn.bin";
    public string WhisperModelType = "BaseEn";
    public bool IgnoreBots = true;
}