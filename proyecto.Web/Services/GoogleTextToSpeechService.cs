using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Configuration;

namespace proyecto.Web.Services;

public class GoogleTextToSpeechService
{
    private readonly TextToSpeechClient _client;

    public GoogleTextToSpeechService(IConfiguration configuration)
    {
        var apiKey = configuration["GoogleTTS:ApiKey"];
        _client = new TextToSpeechClientBuilder { ApiKey = apiKey }.Build();
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
        catch (Exception ex)
        {
            throw new Exception($"Google TTS error: {ex.Message}", ex);
        }
    }
}