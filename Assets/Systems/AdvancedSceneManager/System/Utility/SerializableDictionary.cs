using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    [Serializable]
    internal class SerializableStringBoolDict : SerializableDictionary<string, bool>
    { }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {

        [SerializeField] private bool m_throwOnDeserializeWhenKeyValueMismatch = true;
        public bool throwOnDeserializeWhenKeyValueMismatch
        {
            get => m_throwOnDeserializeWhenKeyValueMismatch;
            set => m_throwOnDeserializeWhenKeyValueMismatch = value;
        }

        [SerializeField]
        protected List<TKey> keys = new List<TKey>();

        [SerializeField]
        protected List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public virtual void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public virtual void OnAfterDeserialize()
        {

            Clear();

            if (keys.Count != values.Count)
            {
                if (throwOnDeserializeWhenKeyValueMismatch)
                    throw new System.Exception(string.Format($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable."));
                return;
            }

            for (int i = 0; i < keys.Count; i++)
                Add(keys[i], values[i]);

        }

    }

}
