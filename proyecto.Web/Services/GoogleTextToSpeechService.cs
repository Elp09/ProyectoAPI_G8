using Google.Cloud.TextToSpeech.V1;

namespace proyecto.Web.Services;

public class GoogleTextToSpeechService
{
    private TextToSpeechClient? _client;

    private TextToSpeechClient GetClient()
    {
        return _client ??= TextToSpeechClient.Create();
    }

    public async Task<byte[]> GenerateSpeechAsync(string text, string languageCode)
    {
        try
        {
            var input = new SynthesisInput
            {
                Text = text
            };

            var voice = new VoiceSelectionParams
            {
                LanguageCode = languageCode

            };

            var config = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var response = await GetClient().SynthesizeSpeechAsync(
                input,
                voice,
                config
            );

            return response.AudioContent.ToByteArray();
        }
        catch
        {
            return null;
        }
    }
}