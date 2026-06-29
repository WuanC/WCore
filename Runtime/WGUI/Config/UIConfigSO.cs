using UnityEngine;
using System.Collections.Generic;
using System;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WGUI
{
    [CreateAssetMenu(menuName = "WGUI/UI Configs")]
    public class UIConfigSO : ScriptableObject
    {

        [SerializeField] private List<UIConfig> _configs;
        private Dictionary<Type, UIConfig> _configDictionary;

        public IReadOnlyList<UIConfig> Configs => _configs;

        public void EnsureDictionary()
        {
            if (_configDictionary != null) return;

            _configDictionary = new Dictionary<Type, UIConfig>();

            foreach (var config in _configs)
            {
                if (string.IsNullOrEmpty(config.TypeName))
                    continue;

                Type type = Type.GetType(config.TypeName);

                if (type == null)
                    continue;

                if (!_configDictionary.TryAdd(type, config))
                {
                    Debug.LogError($"Duplicate UIConfig for type {type.FullName} in {name}");
                }
            }

        }
        public void ClearDictionary()
        {
            _configDictionary?.Clear();
            _configDictionary = null;
        }
        public UIConfig GetConfig(Type type)
        {
            if (type == null)
            {
                Debug.LogError("Type is null");
                return null;
            }
            EnsureDictionary();
            _configDictionary.TryGetValue(type, out UIConfig config);
            return config;
        }
        public int ResolveSortingOrder(UIConfig uiConfig, int defaultOrder)
        {
            if (uiConfig == null)
                return defaultOrder;

            return uiConfig.SortingOrder >= 0
                ? uiConfig.SortingOrder
                : defaultOrder;
        }
#if UNITY_EDITOR

        private void OnValidate()
        {
            foreach (var config in _configs)
            {
                UpdateType(config);
            }
        }

        private void UpdateType(UIConfig config)
        {
            if (config.AssetReference == null)
                return;

            string path = AssetDatabase.GUIDToAssetPath(config.AssetReference.AssetGUID);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                return;

            UIBase ui = prefab.GetComponent<UIBase>();

            if (ui == null)
                return;

            config.TypeName = ui.GetType().AssemblyQualifiedName;
        }

#endif

    }
}