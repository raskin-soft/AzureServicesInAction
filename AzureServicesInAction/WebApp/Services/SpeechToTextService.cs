using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using WebApp.Interfaces;

namespace WebApp.Services
{
    public class SpeechToTextService : ISpeechToTextService
    {
        private readonly string _apiKey;
        private readonly string _region;

        public SpeechToTextService(IConfiguration config)
        {
            _apiKey = config["AzureSpeech:ApiKey"];
            _region = config["AzureSpeech:Region"];
        }

        public async Task<string> TranscribeFromMicrophoneAsync()
        {
            var config = SpeechConfig.FromSubscription(_apiKey, _region);
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(config, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Reason == ResultReason.RecognizedSpeech
                ? result.Text
                : $"Error: {result.Reason}";
        }

        public async Task<string> TranscribeFromFileAsync(string filePath)
        {
            var config = SpeechConfig.FromSubscription(_apiKey, _region);
            using var audioConfig = AudioConfig.FromWavFileInput(filePath);
            using var recognizer = new SpeechRecognizer(config, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Reason == ResultReason.RecognizedSpeech
                ? result.Text
                : $"Error: {result.Reason}";
        }
    }
}
