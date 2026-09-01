"""
Vosk-based speech-to-text transcription.
Usage: python3 -m transcribe.vosk_transcribe <audio_file_path> [language]
"""
import json
import sys
import wave
from pathlib import Path

try:
    import vosk
    VOSK_AVAILABLE = True
except ImportError:
    VOSK_AVAILABLE = False

# Language → Vosk model mapping
MODEL_MAP = {
    "fr": "vosk-model-small-fr-0.22",
    "en": "vosk-model-small-en-us-0.15",
    "ar": "vosk-model-small-ar-0.22",
}

_model_cache = {}


def check_vosk() -> dict:
    return {"available": VOSK_AVAILABLE, "models": list(MODEL_MAP.keys())}


def _get_model(lang: str = "fr"):
    model_name = MODEL_MAP.get(lang, MODEL_MAP["fr"])
    if model_name in _model_cache:
        return _model_cache[model_name]

    if not VOSK_AVAILABLE:
        raise RuntimeError("Vosk n'est pas installé. Exécutez: pip install vosk")

    model = vosk.Model(model_name)
    _model_cache[model_name] = model
    return model


def _convert_to_wav(input_path: str) -> str:
    """Convert audio to 16kHz mono WAV if needed using wave module or ffmpeg."""
    input_p = Path(input_path)
    suffix = input_p.suffix.lower()

    if suffix == ".wav":
        # Check if it's already 16kHz mono
        try:
            with wave.open(str(input_p), "rb") as wf:
                if wf.getframerate() == 16000 and wf.getnchannels() == 1:
                    return str(input_p)
        except Exception:
            pass

    # Try ffmpeg conversion
    output_path = str(input_p.with_suffix(".wav"))
    try:
        import subprocess
        result = subprocess.run(
            ["ffmpeg", "-y", "-i", str(input_p), "-ar", "16000", "-ac", "1", "-f", "wav", output_path],
            capture_output=True, timeout=30
        )
        if result.returncode == 0:
            return output_path
    except (FileNotFoundError, subprocess.TimeoutExpired):
        pass

    return str(input_p)


def transcribe_file(audio_path: str, lang: str = "fr") -> dict:
    if not VOSK_AVAILABLE:
        return {"error": "Vosk n'est pas installé. pip install vosk", "text": ""}

    audio_p = Path(audio_path)
    if not audio_p.exists():
        return {"error": f"Fichier audio introuvable: {audio_path}", "text": ""}

    try:
        wav_path = _convert_to_wav(audio_path)
        model = _get_model(lang)

        with wave.open(wav_path, "rb") as wf:
            rec = vosk.KaldiRecognizer(model, wf.getframerate())
            rec.SetWords(True)

            results = []
            while True:
                data = wf.readframes(4000)
                if len(data) == 0:
                    break
                if rec.AcceptWaveform(data):
                    result = json.loads(rec.Result())
                    if result.get("text"):
                        results.append(result["text"])

            final = json.loads(rec.FinalResult())
            if final.get("text"):
                results.append(final["text"])

            full_text = " ".join(results).strip()
            return {"text": full_text, "lang": lang}

    except Exception as e:
        return {"error": str(e), "text": ""}


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python3 -m transcribe.vosk_transcribe <audio_file> [lang]"}))
        sys.exit(1)

    audio_path = sys.argv[1]
    lang = sys.argv[2] if len(sys.argv) > 2 else "fr"

    result = transcribe_file(audio_path, lang)
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
