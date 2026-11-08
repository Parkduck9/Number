using UnityEditor;
using UnityEngine;

// Import settings for everything under Assets/Audio (decision 10-A: Vorbis, total ~5 MB or less).
// Source files are 32 kHz mono WAV made by Assets/Audio/Source/gen-audio.mjs.txt; Unity encodes them.
// Note: WebGL builds re-encode to AAC whatever the default format says; quality still sets the size.
public class AudioImportRules : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool isBgm = assetPath.Contains("/Bgm/");

        importer.forceToMono = true;
        importer.loadInBackground = isBgm;
        var s = importer.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = isBgm ? 0.45f : 0.6f;
        s.loadType = isBgm ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        s.preloadAudioData = !isBgm;
        importer.defaultSampleSettings = s;
    }
}
