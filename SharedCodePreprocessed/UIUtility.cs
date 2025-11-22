// ReSharper disable CheckNamespace

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace tinygrox.DuckovMods.MoreRageMode.SharedCode;

public class UIUtility
{
    public static readonly string IconFolderPath = Path.Combine(LocalizationUtility.AssemblyDir, "Textures", "Icons");

    public static Sprite LoadIconSpriteFromFileName(string iconFileNameWithExtension)
    {
        string path = Path.Combine(IconFolderPath, iconFileNameWithExtension);
        if (!File.Exists(path))
        {
            return null;
        }

        return LoadSpriteFromPath(path);
    }

    public static Sprite LoadSpriteFromPath(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new(2, 2);
        texture.LoadImage(bytes);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 0f));
    }

    public static async UniTask<List<Sprite>> LoadSpritesFromFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            ModLogger.Log.Error($"[UIUtility] Directory not found for async loading: {folderPath}");
            return new List<Sprite>();
        }

        // 只要 png
        IEnumerable<string> imagePaths = Directory.EnumerateFiles(folderPath, "*.png", SearchOption.AllDirectories);

        List<Sprite> loadedSprites = new();

        foreach (string filePath in imagePaths)
        {
            try
            {
                byte[] fileData = await File.ReadAllBytesAsync(filePath);

                await UniTask.SwitchToMainThread();

                Texture2D texture = new(2, 2);
                if (texture.LoadImage(fileData))
                {
                    Rect rect = new(0, 0, texture.width, texture.height);
                    Vector2 pivot = new(0f, 0.25f);
                    Sprite sprite = Sprite.Create(texture, rect, pivot, 100f);

                    sprite.name = Path.GetFileNameWithoutExtension(filePath);

                    loadedSprites.Add(sprite);
                }
                else
                {
                    ModLogger.Log.Error($"[UIUtility] Failed to load image data from: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Log.Error($"[UIUtility] Error processing file '{filePath}'. Exception: {ex.Message}");
            }
        }

        return loadedSprites;
    }
}

