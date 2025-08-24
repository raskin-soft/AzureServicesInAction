using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;
using WebApp.Models;
using NAudio.Wave;

namespace WebApp.Controllers
{
    public class AIController : Controller
    {
        private readonly ICognitiveTextService _textService;
        private readonly IChatService _chatService;
        private readonly ISpeechToTextService _speechService;
        private readonly ITranslatorService _translationService;

        public AIController(IChatService chatService, ICognitiveTextService textService, ISpeechToTextService speechService, ITranslatorService translationService)
        {
            _chatService = chatService;
            _textService = textService;
            _speechService = speechService;
            _translationService = translationService;
        }

        [HttpGet]
        public IActionResult AIFoundryChat()
        {
            return View(new ChatViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> AIFoundryChat(ChatViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.UserPrompt))
            {
                await Analyze(model.UserPrompt);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(string input)
        {
            var chatReply = await _chatService.GetResponseAsync($"Summarize and analyze: {input}");
            var sentiment = _textService.AnalyzeSentiment(chatReply);
            var keyPhrases = _textService.ExtractKeyPhrases(chatReply);

            ViewBag.ChatReply = chatReply;
            ViewBag.Sentiment = sentiment;
            ViewBag.KeyPhrases = keyPhrases;

            return View();
        }

        [HttpGet]
        public IActionResult Speech()
        {
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> Speech()
        //{
        //    var text = await _speechService.TranscribeFromMicrophoneAsync();
        //    ViewBag.Transcription = text;
        //    return View();
        //}

        [HttpPost]
        [Route("AI/UploadAudio")]
        public async Task<IActionResult> UploadAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid audio file.");
                return View("MicTranscribe");
            }

            // Save to a temporary file
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            var wavPath = ConvertToPcmWav(tempPath);
            // Transcribe using Azure Speech
            var transcription = await _speechService.TranscribeFromFileAsync(tempPath);

            System.IO.File.Delete(wavPath);

            // Clean up temp file
            System.IO.File.Delete(tempPath);

            ViewBag.Transcription = transcription;
            return View("MicTranscribe");
        }



        public string ConvertToPcmWav(string inputPath)
        {
            var outputPath = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
            using var reader = new AudioFileReader(inputPath);
            WaveFileWriter.CreateWaveFile(outputPath, reader);
            return outputPath;
        }

        [HttpGet]
        public IActionResult Translator()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Translator(string inputText, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(inputText) || string.IsNullOrWhiteSpace(targetLang))
            {
                ViewBag.Result = "Please provide text and target language code.";
                return View();
            }

            string translatedText = await _translationService.TranslateTextAsync(inputText, targetLang);
            ViewBag.Result = translatedText;
            ViewBag.InputText = inputText;
            ViewBag.TargetLang = targetLang;

            return View();
        }


    }
}
