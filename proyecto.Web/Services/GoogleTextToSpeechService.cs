using Google.Cloud.TextToSpeech.V1;

namespace proyecto.Web.Services;

public class GoogleTextToSpeechService
{
    private readonly TextToSpeechClient _client;

    public GoogleTextToSpeechService()
    {
        _client = TextToSpeechClient.Create();
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

            var response = await _client.SynthesizeSpeechAsync(
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