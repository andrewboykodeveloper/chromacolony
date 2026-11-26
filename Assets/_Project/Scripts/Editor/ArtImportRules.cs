using UnityEditor;

namespace Chromacolony.Editor
{
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/_Project/Art/Sprites/";
        public const float PixelsPerUnit = 512f;

        public static readonly string[] Phones = { "Android", "iPhone" };

        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            foreach (string phone in Phones)
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(phone);
                settings.overridden = true;
                settings.format = TextureImporterFormat.ASTC_4x4;
                settings.maxTextureSize = importer.maxTextureSize;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
