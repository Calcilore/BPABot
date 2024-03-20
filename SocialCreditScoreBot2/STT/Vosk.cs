using System.Text.Json.Nodes;
using Vosk;

namespace SocialCreditScoreBot2.Implementations;

public class Vosk : ISpeechToText {
    private static Model model;
    
    public Task<bool> Init(string modelPath) {
        model = new Model("models/" + modelPath);
        return Task.FromResult(true);
    }

    public Task<string> Synthesize(byte[] data) {
        VoskRecognizer rec = new VoskRecognizer(model, 44100.0f);
        rec.SetMaxAlternatives(0);
        rec.SetWords(true);

        rec.AcceptWaveform(data, data.Length);

        return Task.FromResult(JsonNode.Parse(rec.FinalResult())["text"].ToString());
    }
}