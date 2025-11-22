// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.Events;

namespace tinygrox.DuckovMods.MoreRageMode.SharedCode;

public class InteractableHelper
{
    private static readonly Func<InteractableBase, List<InteractableBase>> s_getOtherInteractables = ReflectionHelper.CreateFieldGetter<InteractableBase, List<InteractableBase>>("otherInterablesInGroup");
    private static readonly Action<InteractableBase, List<InteractableBase>> s_setOtherInteractables = ReflectionHelper.CreateFieldSetter<InteractableBase, List<InteractableBase>>("otherInterablesInGroup");
    private static readonly Func<BaseBGMSelector, DialogueBubbleProxy> s_getProxy = ReflectionHelper.CreateFieldGetter<BaseBGMSelector, DialogueBubbleProxy>("proxy");

    /// <summary>
    /// 基于一个模板 InteractableBase 创建并配置一个新的 InteractableBase 对象。
    /// </summary>
    /// <param name="parentTransform">parentTransform 必须是拥有 InteractableBase 的物体</param>
    /// <param name="templateInteractable"></param>
    /// <param name="interactName"></param>
    /// <param name="onInteractStartAction"></param>
    /// <param name="onInteractFinishedAction"></param>
    /// <param name="onInteractTimeoutAction"></param>
    /// <param name="onRequiredItemUsedAction"></param>
    /// <param name="requireItem"></param>
    /// <param name="disableOnFinish"></param>
    /// <returns></returns>
    public static InteractableBase CreateConfiguredInteractable
    (
        Transform parentTransform,
        InteractableBase templateInteractable,
        string interactName,
        UnityAction<CharacterMainControl, InteractableBase> onInteractStartAction = null,
        UnityAction<CharacterMainControl, InteractableBase> onInteractFinishedAction = null,
        UnityAction<CharacterMainControl, InteractableBase> onInteractTimeoutAction = null,
        UnityAction onRequiredItemUsedAction = null,
        bool requireItem = false,
        bool disableOnFinish = false
    )
    {
        if (templateInteractable is null)
        {
            Debug.LogError("[InteractableCreationHelper] Template InteractableBase 为空。无法创建新的可交互对象。");
            return null;
        }

        GameObject newInteractableGameObject = UnityEngine.Object.Instantiate(templateInteractable.gameObject, parentTransform);
        newInteractableGameObject.name = $"GeneratedInteractable_{interactName.Replace(" ", "")}";

        newInteractableGameObject.transform.localPosition = Vector3.zero;
        newInteractableGameObject.transform.localRotation = Quaternion.identity;
        newInteractableGameObject.transform.localScale = Vector3.one;

        InteractableBase newInteractable = newInteractableGameObject.GetComponent<InteractableBase>();
        if (newInteractable is null)
        {
            Debug.LogError($"[InteractableCreationHelper] 从实例化的对象 '{newInteractableGameObject.name}' 中获取 InteractableBase 组件失败。正在销毁 GameObject。");
            UnityEngine.Object.Destroy(newInteractableGameObject);
            return null;
        }

        newInteractable.overrideInteractName = true;
        newInteractable.InteractName = interactName; // 无需 ToPlainText()
        newInteractable.requireItem = requireItem;
        newInteractable.disableOnFinish = disableOnFinish;

        newInteractable.OnInteractStartEvent = new UnityEvent<CharacterMainControl, InteractableBase>();
        if (onInteractStartAction != null)
        {
            newInteractable.OnInteractStartEvent.AddListener(onInteractStartAction);
        }

        if (onInteractFinishedAction != null)
        {
            newInteractable.OnInteractFinishedEvent = new UnityEvent<CharacterMainControl, InteractableBase>();
            newInteractable.OnInteractFinishedEvent.AddListener(onInteractFinishedAction);
        }
        else
        {
            newInteractable.OnInteractFinishedEvent = null;
        }

        if (onInteractTimeoutAction != null)
        {
            newInteractable.OnInteractTimeoutEvent = new UnityEvent<CharacterMainControl, InteractableBase>();
            newInteractable.OnInteractTimeoutEvent.AddListener(onInteractTimeoutAction);
        }
        else
        {
            newInteractable.OnInteractTimeoutEvent = null;
        }

        if (onRequiredItemUsedAction != null)
        {
            newInteractable.OnRequiredItemUsedEvent = new UnityEvent();
            newInteractable.OnRequiredItemUsedEvent.AddListener(onRequiredItemUsedAction);
        }
        else
        {
            newInteractable.OnRequiredItemUsedEvent = null;
        }

        return newInteractable;
    }
}

