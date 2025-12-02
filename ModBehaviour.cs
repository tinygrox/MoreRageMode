using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Duckov.Rules;
using Duckov.Utilities;
using HarmonyLib;
using Newtonsoft.Json;
using Sirenix.Utilities;
using SodaCraft.Localizations;
using tinygrox.DuckovMods.MoreRageMode.SharedCode;

namespace tinygrox.DuckovMods.MoreRageMode;

public class ModBehaviour : Duckov.Modding.ModBehaviour
{
    public class RageConfig
    {
        public float EnemyHealthFactor = 1.5f;
        public float EnemyReactionTimeFactor = 0.1f;
        public float EnemyAttackTimeSpaceFactor = 0.2f;
        public float GunScatterMultiplier = 0.01f;
        // 这里影响的是角色枪械暴击率 原版计算公式大概为：暴击率 = 枪械暴击率 x (1 + 角色枪械暴击率 + 弹药暴击增益)
        // 枪械暴击率(ItemAgen_Gun.CritRate) 根据具体枪械为准，各种数值都有，比如 AK74U 为 0.15，PM 手枪为 0.3
        // 角色枪械暴击率(CharacterMainControl.CharacterItem.GetStatValue("GunCritRateGain".GetHashCode())) 这个是对面爆你的概率
        // 弹药暴击增益(Item.Constants.GetFloat("CritRateGain"))通常为 0
        // 注：当暴击率 > 0.99 时，无视沙袋等障碍物
        public float GunCritRateGain = 2f;
    }

    private RageConfig _config;
    private const string ConfigFileName = "config.json";

    private float _originalEnemyHealthFactor;
    private float _originalEnemyReactionTimeFactor;
    private float _originalEnemyAttackTimeSpaceFactor;
    private readonly Dictionary<CharacterRandomPreset, float> _originalGunScatterMultipliers = new();
    private readonly Dictionary<CharacterRandomPreset, float> _originalGunCritRateGain = new();

    protected override void OnAfterSetup()
    {
        ModLogger.Instance.DefaultModName = "tinygrox.DuckovMods.MoreRageMode";

        LoadConfig();

        ApplyOrRestoreRuleRage(true);
        HandleRuleChange();
        GameRulesManager.OnRuleChanged += HandleRuleChange;
    }

    private void LoadConfig()
    {
        string configPath = Path.Combine(info.path, ConfigFileName);
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                _config = JsonConvert.DeserializeObject<RageConfig>(json);
                ModLogger.Log.Debug($"Loaded config from {configPath}");
            }
            catch (Exception e)
            {
                ModLogger.Log.Error($"Failed to load config: {e.Message}");
                _config = new RageConfig();
            }
        }
        else
        {
            _config = new RageConfig();
            try
            {
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                ModLogger.Log.Debug($"Created default config at {configPath}");
            }
            catch (Exception e)
            {
                ModLogger.Log.Error($"Failed to save default config: {e.Message}");
            }
        }
    }

    protected override void OnBeforeDeactivate()
    {
        GameRulesManager.OnRuleChanged -= HandleRuleChange;
        ApplyOrRestoreRuleRage(false);
        Restore(true);
    }

    private void Restore(bool deactivate = false)
    {
        if(GameRulesManager.Current.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase) && !deactivate) return;

        if (_originalGunScatterMultipliers.Count > 0)
        {
            foreach (var entry in _originalGunScatterMultipliers)
            {
                entry.Key.gunScatterMultiplier = entry.Value;
            }

            ModLogger.Log.Debug("Restored all original gunScatterMultiplier values.");
            _originalGunScatterMultipliers.Clear();
        }

        if (_originalGunCritRateGain.Count > 0)
        {
            foreach (KeyValuePair<CharacterRandomPreset, float> entry in _originalGunCritRateGain)
            {
                entry.Key.gunCritRateGain = entry.Value;
            }
            ModLogger.Log.Debug("Restored all original gunCritRateGain values.");
            _originalGunCritRateGain.Clear();
        }
    }

    // 只对狂暴模式做修改，且一次修改全局生效。
    private void ApplyOrRestoreRuleRage(bool applyChanges)
    {
        var entries = (IEnumerable)Traverse.Create(GameRulesManager.Instance).Field("entries").GetValue();

        foreach (object entryObj in entries)
        {
            RulesetFile rulesetFile = Traverse.Create(entryObj).Field("file").GetValue<RulesetFile>();
            Ruleset ruleset = rulesetFile.Data;

            if (!ruleset.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase)) continue;

            if (applyChanges)
            {
                _originalEnemyHealthFactor = Traverse.Create(ruleset).Field("enemyHealthFactor").GetValue<float>();
                _originalEnemyReactionTimeFactor = Traverse.Create(ruleset).Field("enemyReactionTimeFactor").GetValue<float>();
                _originalEnemyAttackTimeSpaceFactor = Traverse.Create(ruleset).Field("enemyAttackTimeSpaceFactor").GetValue<float>();

                Traverse.Create(ruleset).Field("enemyHealthFactor").SetValue(_config.EnemyHealthFactor); // 肉死你
                Traverse.Create(ruleset).Field("enemyReactionTimeFactor").SetValue(_config.EnemyReactionTimeFactor); // 秒你
                Traverse.Create(ruleset).Field("enemyAttackTimeSpaceFactor").SetValue(_config.EnemyAttackTimeSpaceFactor); // 射爆你
                ModLogger.Log.Debug($"Updated Rule_Rage: EnemyHealthFactor={_config.EnemyHealthFactor}, EnemyReactionTimeFactor={_config.EnemyReactionTimeFactor}, EnemyAttackTimeSpaceFactor={_config.EnemyAttackTimeSpaceFactor}");
            }
            else
            {
                Traverse.Create(ruleset).Field("enemyHealthFactor").SetValue(_originalEnemyHealthFactor);
                Traverse.Create(ruleset).Field("enemyReactionTimeFactor").SetValue(_originalEnemyReactionTimeFactor);
                Traverse.Create(ruleset).Field("enemyAttackTimeSpaceFactor").SetValue(_originalEnemyAttackTimeSpaceFactor);
                ModLogger.Log.Debug($"Restored Rule_Rage: EnemyHealthFactor={_originalEnemyHealthFactor}, EnemyReactionTimeFactor={_originalEnemyReactionTimeFactor}, EnemyAttackTimeSpaceFactor={_originalEnemyAttackTimeSpaceFactor}");
            }
        }
        GameRulesManager.NotifyRuleChanged();
        ModLogger.Log.Debug($"Current rule: {GameRulesManager.Current.DisplayName}");
    }

    private void HandleRuleChange()
    {
        if (!GameRulesManager.Current.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase))
        {
            Restore();
            return;
        }

        if (GameplayDataSettings.CharacterRandomPresetData?.presets == null)
        {
            ModLogger.Log.Debug("谁把 GameplayDataSettings.CharacterRandomPresetData.presets 删了鸭");
            return;
        }

        foreach (CharacterRandomPreset characterRandomPreset in GameplayDataSettings.CharacterRandomPresetData.presets)
        {
            if (characterRandomPreset.nameKey.IsNullOrWhitespace() || characterRandomPreset.team == Teams.player) continue;

            if (!_originalGunScatterMultipliers.ContainsKey(characterRandomPreset))
            {
                if (characterRandomPreset.gunScatterMultiplier > _config.GunScatterMultiplier)
                {
                    _originalGunScatterMultipliers[characterRandomPreset] = characterRandomPreset.gunScatterMultiplier;
                    characterRandomPreset.gunScatterMultiplier = _config.GunScatterMultiplier; // 准死你
                }
            }

            if (!_originalGunCritRateGain.ContainsKey(characterRandomPreset))
            {
                if (characterRandomPreset.gunCritRateGain < _config.GunCritRateGain)
                {
                    _originalGunCritRateGain[characterRandomPreset] = characterRandomPreset.gunCritRateGain;
                    characterRandomPreset.gunCritRateGain = _config.GunCritRateGain; // 爆头爆死你
                }
            }

        }

        ModLogger.Log.Debug($"Set gunScatterMultiplier to {_config.GunScatterMultiplier} for some CharacterRandomPresets.");
    }
}
