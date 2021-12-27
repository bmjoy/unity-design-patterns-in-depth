#if UNITY_EDITOR

using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>
    /// <para>An utility for registering to build events.</para>
    /// <para>Only available in editor.</para>
    /// </summary>
    public static class BuildEventsUtility
    {

        public delegate void BuildError(string condition, string stacktrace, LogType type);

        /// <summary>Occurs before build.</summary>
        public static event Action preBuild;

        /// <summary>Occurs after build.</summary>
        public static event Action postBuild;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event Action onError;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event BuildError onErrorWithArgs;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event BuildError onWarningWithArgs;

        class BuildEvents : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport _)
            {
                Application.logMessageReceived += OnBuildError;
                preBuild?.Invoke();
            }

            public void OnPostprocessBuild(BuildReport _)
            {
                Application.logMessageReceived -= OnBuildError;
                postBuild?.Invoke();
            }

            public void OnBuildError(string condition, string stacktrace, LogType type)
            {
                if (type != LogType.Error)
                    onWarningWithArgs?.Invoke(condition, stacktrace, type);
                else
                {
                    onErrorWithArgs?.Invoke(condition, stacktrace, type);
                    onError?.Invoke();
                }
            }

        }

    }

}
#endif
