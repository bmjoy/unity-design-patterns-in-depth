using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace AdvancedSceneManager.Utility
{

    internal static class ActionUtility
    {

        /// <inheritdoc cref="Try(Action, out Exception, bool)"/>
        public static void Try(this Action action) =>
            Try(action, out _);

        /// <summary>Wraps the call in a try catch block, perhaps not the best practice, but makes invoking user code much cleaner.</summary>
        public static void Try(this Action action, out Exception exception, bool writeToLog = true)
        {

            exception = null;

            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                if (writeToLog)
                    Debug.LogError(e);
                exception = e;
            }

        }

    }

}
