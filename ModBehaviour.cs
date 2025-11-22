using System;
using System.Collections;
using System.Collections.Generic; // Added for Dictionary
using Duckov.Rules;
using Duckov.Utilities;
using HarmonyLib; // Re-added for Traverse
using Sirenix.Utilities; // For IsNullOrWhitespace
using SodaCraft.Localizations;
using tinygrox.DuckovMods.MoreRageMode.SharedCode;

namespace tinygrox.DuckovMods.MoreRageMode;

public class ModBehaviour : Duckov.Modding.ModBehaviour
{
    private float _originalEnemyHealthFactor;
    private float _originalEnemyReactionTimeFactor;
    private float _originalEnemyAttackTimeSpaceFactor;
    private readonly Dictionary<CharacterRandomPreset, float> _originalGunScatterMultipliers = new();
    private readonly Dictionary<CharacterRandomPreset, float> _originalGunCritRateGain = new();
    private const float SetEnemyHealthFactor = 1.5f;
    private const float SetEnemyReactionTimeFactor = 0.1f;
    private const float SetEnemyAttackTimeSpaceFactor = 0.1f;

    protected override void OnAfterSetup()
    {
        ModLogger.Instance.DefaultModName = "tinygrox.DuckovMods.MoreRageMode";

        ApplyOrRestoreRuleRage(true);
        HandleGunScatterMultiplierChange();
        GameRulesManager.OnRuleChanged += HandleGunScatterMultiplierChange;
    }

    protected override void OnBeforeDeactivate()
    {
        GameRulesManager.OnRuleChanged -= HandleGunScatterMultiplierChange; // Unsubscribe from event
        ApplyOrRestoreRuleRage(false); // Restore Rule original values
        RestoreGunScatterMultipliers(); // Restore all gunScatterMultiplier values
    }

    private void RestoreGunScatterMultipliers()
    {
        if(GameRulesManager.Current.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase)) return;

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
            var rulesetFile = Traverse.Create(entryObj).Field("file").GetValue<RulesetFile>();
            var ruleset = rulesetFile.Data;

            if (!ruleset.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase)) continue;

            if (applyChanges)
            {
                _originalEnemyHealthFactor = Traverse.Create(ruleset).Field("enemyHealthFactor").GetValue<float>();
                _originalEnemyReactionTimeFactor = Traverse.Create(ruleset).Field("enemyReactionTimeFactor").GetValue<float>();
                _originalEnemyAttackTimeSpaceFactor = Traverse.Create(ruleset).Field("enemyAttackTimeSpaceFactor").GetValue<float>();

                Traverse.Create(ruleset).Field("enemyHealthFactor").SetValue(SetEnemyHealthFactor); // 肉死你
                Traverse.Create(ruleset).Field("enemyReactionTimeFactor").SetValue(SetEnemyReactionTimeFactor); // 秒你
                Traverse.Create(ruleset).Field("enemyAttackTimeSpaceFactor").SetValue(SetEnemyAttackTimeSpaceFactor); // 射爆你
                ModLogger.Log.Debug($"Updated Rule_Rage: EnemyHealthFactor={SetEnemyHealthFactor}, EnemyReactionTimeFactor={SetEnemyReactionTimeFactor}, EnemyAttackTimeSpaceFactor={SetEnemyAttackTimeSpaceFactor}");
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

    private void HandleGunScatterMultiplierChange()
    {
        if (!GameRulesManager.Current.DisplayName.Equals("Rule_Rage".ToPlainText(), StringComparison.OrdinalIgnoreCase))
        {
            RestoreGunScatterMultipliers();
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
                _originalGunScatterMultipliers[characterRandomPreset] = characterRandomPreset.gunScatterMultiplier;
                _originalGunCritRateGain[characterRandomPreset] = characterRandomPreset.gunCritRateGain;
            }

            characterRandomPreset.gunScatterMultiplier = 0.01f; // 准死你
            characterRandomPreset.gunCritRateGain = 100f; // 爆头爆死你
        }

        ModLogger.Log.Debug("Set gunScatterMultiplier to 0.01f for all CharacterRandomPresets (excluding player and empty display names).");
    }
}
