using System.Diagnostics;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace SocialCreditScoreBot2.Implementations;

public class Whisper : ISpeechToText {
    private WhisperFactory whisperFactory;
    
    public async Task<bool> Init(string modelPath) {
        if (!File.Exists(modelPath)) {
            Logging.Warning("Model not found, downloading...");
            if (!Enum.TryParse(Program.Config.WhisperModelType, true, out GgmlType type)) {
                Logging.Error("Invalid WhisperModelType in config, valid options are: Tiny, TinyEn, Small, SmallEn, Base, BaseEn, Medium, MediumEm, LargeV1, LargeV2, LargeV3");
                return false;
            }

            // create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath));
            
            await using Stream modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(type);
            await using FileStream fileWriter = File.OpenWrite(modelPath);
            await modelStream.CopyToAsync(fileWriter);
        }
        
        whisperFactory = WhisperFactory.FromPath(modelPath);
        
        return true;
    }

    public async Task<string> Synthesize(byte[] data) {
        Stopwatch sw = Stopwatch.StartNew();
        // the byte array is 48kHz s16le PCM,
        // we need to convert it to 16kHz float PCM
        // 16kHz is 1/3 of 48kHz, so we can just take every third sample
        float[] floatData = new float[data.Length / 6]; // 2 bytes per sample, reading every third sample
        for (int i = 0; i < floatData.Length; i++) {
            floatData[i] = BitConverter.ToInt16(data, i * 6) / (float)short.MaxValue;
        }

        WhisperProcessor processor = whisperFactory.CreateBuilder()
            .WithLanguage("en")
            .Build();
        
        StringBuilder result = new StringBuilder();
        await foreach (SegmentData segment in processor.ProcessAsync(floatData)) {
            result.Append(segment.Text);
        }

        if (sw.ElapsedMilliseconds > Program.Config.STTSpeedThreshold) {
            Logging.Warning("Synthesis was slow, took " + sw.ElapsedMilliseconds + "ms");
        }
        else {
            Logging.Debug("Synthesized in " + sw.ElapsedMilliseconds + "ms");
        }

        return result.ToString();
    }
}