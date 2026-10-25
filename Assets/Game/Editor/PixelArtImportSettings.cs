// Assets/Game/Art 안의 그림은 자동으로 "도트 그림" 설정(Point 필터, 압축 없음)으로 가져옵니다.
using UnityEditor;
using UnityEngine;

public class PixelArtImportSettings : AssetPostprocessor
{
    public const string ArtFolder = "Assets/Game/Art";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtFolder)) return;
        Apply((TextureImporter)assetImporter, assetPath);
    }

    void OnPreprocessAsset()
    {
        if (assetPath.StartsWith("Assets/Game/Fonts") && assetImporter is TrueTypeFontImporter f)
        {
            f.fontTextureCase = FontTextureCase.Dynamic;
            f.fontRenderingMode = FontRenderingMode.HintedRaster;
        }
    }

    public static bool Apply(TextureImporter ti, string path)
    {
        bool noise = path.Contains("fx_noise");
        bool changed = ti.textureType != TextureImporterType.Sprite
                       || ti.filterMode != FilterMode.Point
                       || ti.textureCompression != TextureImporterCompression.Uncompressed
                       || ti.mipmapEnabled
                       || (noise && ti.wrapMode != TextureWrapMode.Repeat);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.spritePixelsPerUnit = 4;
        ti.wrapMode = noise ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        return changed;
    }
}
