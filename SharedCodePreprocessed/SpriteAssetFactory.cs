using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

// ReSharper disable CheckNamespace
// ReSharper disable ShaderLabShaderReferenceNotResolved

namespace tinygrox.DuckovMods.MoreRageMode.SharedCode;

/// <summary>
/// 基于密教模拟器的 Roost Mod 改，用于创建 TextSprite
/// </summary>
public class SpriteAssetFactory
{
    private static readonly Action<TMP_SpriteAsset, List<TMP_SpriteCharacter>> s_setSpriteCharacterTable =
        ReflectionHelper.CreateFieldSetter<TMP_SpriteAsset, List<TMP_SpriteCharacter>>("m_SpriteCharacterTable");

    private static readonly Action<TMP_SpriteAsset, List<TMP_SpriteGlyph>> s_setSpriteGlyphTable =
        ReflectionHelper.CreateFieldSetter<TMP_SpriteAsset, List<TMP_SpriteGlyph>>("m_SpriteGlyphTable");

    private static readonly Action<TMP_SpriteAsset, string> s_setVersion =
        ReflectionHelper.CreateFieldSetter<TMP_SpriteAsset, string>("m_Version");

    // 用于在打包过程中追踪每个 Sprite 的信息
    private struct PackedSpriteInfo
    {
        public RectInt DestinationRect;
        public Sprite OriginalSprite;
    }

    /// <summary>
    /// 从一组 Sprite 异步创建一个 TMP_SpriteAsset。这是该类的主要入口点。
    /// </summary>
    public static async UniTask<TMP_SpriteAsset> CreateFromSpritesAsync(IEnumerable<Sprite> sourceSprites)
    {
        List<Sprite> spriteList = sourceSprites?.ToList();
        if (spriteList == null || spriteList.Count == 0)
        {
            ModLogger.Log.Error("[SpriteAssetFactory] The provided sprite list is null or empty.");
            return null;
        }

        (Texture2D atlas, List<PackedSpriteInfo> packedInfos) = await PackSpritesToAtlasAsync(spriteList);
        if (atlas is null)
        {
            ModLogger.Log.Error("[SpriteAssetFactory] Failed to pack sprite atlas texture.");
            return null;
        }

        List<Sprite> spritesInAtlas = new();
        foreach (PackedSpriteInfo info in packedInfos)
        {
            Sprite newSprite = Sprite.Create(
                atlas,
                new Rect(info.DestinationRect.x, info.DestinationRect.y, info.DestinationRect.width, info.DestinationRect.height),
                new Vector2(0f, 0.25f)
            );
            newSprite.name = info.OriginalSprite.name;
            spritesInAtlas.Add(newSprite);
        }

        return CreateSpriteAssetInternal(atlas, spritesInAtlas);
    }

    /// <summary>
    /// 将输入的 Sprite 列表打包成 atlas
    /// </summary>
    private static async UniTask<(Texture2D, List<PackedSpriteInfo>)> PackSpritesToAtlasAsync(List<Sprite> sprites)
    {
        await UniTask.SwitchToThreadPool();
        List<Sprite> spritesToPack = sprites.Where(s => s?.texture != null).OrderByDescending(s => s.rect.height).ToList();
        if (spritesToPack.Count == 0)
        {
            ModLogger.Log.Error("[SpriteAssetFactory] All provided sprites were null or had no texture.");
            return (null, null);
        }

        List<PackedSpriteInfo> packedInfos = new();
        int atlasWidth = 1024;
        int currentX = 0;
        int currentY = 0;
        int rowMaxHeight = 0;
        foreach (Sprite sprite in spritesToPack)
        {
            if (currentX + sprite.rect.width > atlasWidth)
            {
                currentY += rowMaxHeight;
                currentX = 0;
                rowMaxHeight = 0;
            }

            packedInfos.Add(new PackedSpriteInfo { OriginalSprite = sprite, DestinationRect = new RectInt(currentX, currentY, (int)sprite.rect.width, (int)sprite.rect.height) });
            currentX += (int)sprite.rect.width;
            rowMaxHeight = Mathf.Max(rowMaxHeight, (int)sprite.rect.height);
        }

        int atlasHeight = currentY + rowMaxHeight;
        await UniTask.SwitchToMainThread();

        RenderTexture rt = RenderTexture.GetTemporary(atlasWidth, atlasHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

        Graphics.SetRenderTarget(rt);
        GL.Clear(true, true, Color.clear);

        foreach (PackedSpriteInfo info in packedInfos)
        {
            Graphics.CopyTexture(
                info.OriginalSprite.texture,
                0,
                0,
                (int)info.OriginalSprite.rect.x,
                (int)info.OriginalSprite.rect.y,
                (int)info.OriginalSprite.rect.width,
                (int)info.OriginalSprite.rect.height,
                rt,
                0,
                0,
                info.DestinationRect.x,
                info.DestinationRect.y
            );
        }

        Texture2D atlasTexture = new(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);

        RenderTexture.active = rt;
        atlasTexture.ReadPixels(new Rect(0, 0, atlasWidth, atlasHeight), 0, 0);
        atlasTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return (atlasTexture, packedInfos);
    }

    /// <summary>
    /// 实际创建并填充 TMP_SpriteAsset 对象的内部方法。
    /// </summary>
    private static TMP_SpriteAsset CreateSpriteAssetInternal(Texture sourceTex, List<Sprite> spritesInAtlas)
    {
        TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        s_setVersion(spriteAsset, "1.1.0");

        spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(sourceTex.name);
        spriteAsset.spriteSheet = sourceTex;
        Material material = new(Shader.Find("TextMeshPro/Sprite"));
        material.SetTexture(ShaderUtilities.ID_MainTex, sourceTex);
        material.name = "RuntimeSpriteAssetMaterial";
        spriteAsset.material = material;

        List<TMP_SpriteGlyph> spriteGlyphTable = new();
        List<TMP_SpriteCharacter> spriteCharacterTable = new();

        uint index = 0;
        uint unicodeStart = 0xE000;
        foreach (Sprite sprite in spritesInAtlas)
        {
            TMP_SpriteGlyph spriteGlyph = new()
            {
                index = index,
                metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width),
                glyphRect = new GlyphRect(sprite.rect),
                scale = 1.0f,
                sprite = sprite
            };
            spriteGlyphTable.Add(spriteGlyph);

            uint unicode = unicodeStart + index;
            TMP_SpriteCharacter spriteCharacter = new(unicode, spriteGlyph) { name = sprite.name, scale = 1.0f };
            spriteCharacterTable.Add(spriteCharacter);
            index++;
        }

        s_setSpriteGlyphTable(spriteAsset, spriteGlyphTable);
        s_setSpriteCharacterTable(spriteAsset, spriteCharacterTable);

        spriteAsset.SortGlyphTable();
        spriteAsset.UpdateLookupTables();

        return spriteAsset;
    }
}

