using UnityEditor;
using UnityEngine;

namespace Chromacolony.Editor
{
    public sealed class AudioImportRules : AssetPostprocessor
    {
        public const string MusicFolder = "Assets/_Project/Audio/Music/";

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(MusicFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.loadInBackground = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
