using UnityEditor;

namespace Chromacolony.Editor
{
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/_Project/Art/Sprites/";
        public const float PixelsPerUnit = 512f;

        public override uint GetVersion()
        {
            return 3;
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
        }
    }
}
