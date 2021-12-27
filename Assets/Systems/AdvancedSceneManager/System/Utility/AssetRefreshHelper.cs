#if UNITY_EDITOR

using System;

namespace AdvancedSceneManager.Editor.Utility
{

    internal static class AssetRefreshHelper
    {

        public static event Action OnRefreshRequest;

        public static void Refresh() =>
            OnRefreshRequest();

    }

}
#endif
