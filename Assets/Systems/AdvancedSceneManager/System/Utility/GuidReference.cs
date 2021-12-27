#pragma warning disable IDE0051 // Remove unused private members

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Represents a persistent reference to the <see cref="GameObject"/> that this is attached to, see also <see cref="GuidReferenceUtility"/> .</summary>
    public class GuidReference : MonoBehaviour
    {

        public string guid = GenerateID();

        void OnValidate()
        {
            if (!enabled)
                enabled = true;
        }

        private void Start() =>
            GuidReferenceUtility.Add(this);

        private void OnDestroy() =>
            GuidReferenceUtility.Remove(this);

        static string GenerateID() =>
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');

#if UNITY_EDITOR

        [CustomEditor(typeof(GuidReference))]
        public class Editor : UnityEditor.Editor
        {

            public override void OnInspectorGUI()
            { }

            public override bool UseDefaultMargins() =>
                false;

        }

#endif

    }

}
