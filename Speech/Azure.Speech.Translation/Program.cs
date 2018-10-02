using System.IO;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using static System.Console;

namespace Azure.Speech.Translation
{
    class Program
    {
        static void Main(string[] args)
        {
            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var audioPath = Path.Combine(root, @"Audio/HelloImIris.wav");
            var outPath = Path.Combine(root, @"Audio/TranslatedHelloImIris.wav");

            var from = "fr-FR";
            var to = "en-US";
            var voice = "en-US-JessaRUS";

            WriteLine("Translating the following audio");

            new SoundPlayer(audioPath).Play();

            TranslateAsync(from,to,voice,audioPath,outPath).Wait();

            WriteLine("Please press a key to continue.");
            ReadLine();
        }

        static string _key = "Key";
        static string _region = "westus";

        public static string Connected = "Connection open";
        public static string AudioSent = "Audio sent";
        public static string Waiting = "Waiting for response";

        public static async Task TranslateAsync(string from, string to,string voice,string inFile,string outFile)
        {
            var config = SpeechTranslationConfig.FromSubscription(_key, _region);
            config.SpeechRecognitionLanguage = from;
            config.VoiceName = voice;
            config.AddTargetLanguage(to);

            var translationCompleted = new TaskCompletionSource<int>();

            using (var audioInput = AudioConfig.FromWavFileInput(inFile))
            {
                using (var recognizer = new TranslationRecognizer(config, audioInput))
                {
                    recognizer.Recognized += (s, e) => {

                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            WriteLine($"Recognized: {e.Result.Text}");

                            foreach (var element in e.Result.Translations)
                                WriteLine($"Translated: {element.Value}");
                        }
                    };

                    recognizer.Synthesizing += (sender, args) =>
                    {
                        if (args.Result.Reason == ResultReason.SynthesizingAudio)
                        {
                            var bytes = args.Result.GetAudio();

                            if (bytes.Length > 0)
                            {
                                using (var fileStream = File.Create(outFile))
                                    fileStream.Write(bytes, 0, bytes.Length);

                                new SoundPlayer(outFile).Play();

                                WriteLine($"Audio translation can be found here {outFile}");
                            }
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        WriteLine($"Cancelled: {e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                            WriteLine($"Error: {e.ErrorDetails}");

                        translationCompleted.TrySetResult(0);
                    };

                    await recognizer.StartContinuousRecognitionAsync();

                    await translationCompleted.Task;

                    await recognizer.StopContinuousRecognitionAsync();
                }
            }
        }
    }
}
