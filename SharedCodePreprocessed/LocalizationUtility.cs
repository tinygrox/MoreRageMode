// ReSharper disable CheckNamespace

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using SodaCraft.Localizations;
using UnityEngine;

namespace tinygrox.DuckovMods.MoreRageMode.SharedCode;

public static class LocalizationUtility
{
    public static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public static void LoadLanguageFile(SystemLanguage language)
    {
        string langName = language.ToString();
        string langFilePath = GetLocalizationFilePath(langName);
        if (!File.Exists(langFilePath))
        {
            langFilePath = GetLocalizationFilePath("English");

            if (!File.Exists(langFilePath))
            {
                return;
            }
        }

        string jsonContent = File.ReadAllText(langFilePath);
        Dictionary<string, string> localizedStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

        if (localizedStrings == null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> pair in localizedStrings)
        {
            LocalizationManager.SetOverrideText(pair.Key, pair.Value);
        }
    }

    private static string GetLocalizationFilePath(string langName) => AssemblyDir != null ? Path.Combine(AssemblyDir, "Localization", $"{langName}.json") : null;
}

