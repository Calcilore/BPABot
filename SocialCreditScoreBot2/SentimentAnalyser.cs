using VaderSharp2;
using Vosk;

namespace SocialCreditScoreBot2;

public static class SentimentAnalyser {
    private static Model model;
    
    public static void Init(string modelPath) {
        model = new Model("models/" + modelPath);
        
        Console.WriteLine("Started Speech Detector");
    }

    public static string Synthesize(byte[] data) {
        VoskRecognizer rec = new VoskRecognizer(model, 44100.0f);
        rec.SetMaxAlternatives(0);
        rec.SetWords(true);

        rec.AcceptWaveform(data, data.Length);

        return rec.FinalResult();
    }
    
    public static float Analyse(string text) {
        SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
        
        SentimentAnalysisResults results = analyzer.PolarityScores(text);

        return (float)results.Compound;
    }
}