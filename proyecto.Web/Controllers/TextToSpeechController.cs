using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Services;

namespace proyecto.Web.Controllers
{
    [ApiController]
    [Route("api/tts")]
    public class TextToSpeechController : Controller
    {
        private readonly GoogleTextToSpeechService _ttsService;

        public TextToSpeechController(GoogleTextToSpeechService ttsService)
        {
            _ttsService = ttsService;
        }

        public class TtsRequest
        {
            public string Text { get; set; }
            public string Language { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Speak([FromBody] TtsRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("No hay texto.");

            var language = string.IsNullOrWhiteSpace(request.Language)
                ? "es-ES"
                : request.Language;

            var audio = await _ttsService.GenerateSpeechAsync(
                request.Text,
                language
            );

            if (audio == null || audio.Length == 0)
                return StatusCode(204);

            return File(audio, "audio/mpeg");
        }
    }
}