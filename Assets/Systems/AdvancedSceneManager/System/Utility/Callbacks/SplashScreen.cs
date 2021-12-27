using AdvancedSceneManager.Core;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>
    /// <para>A class that contains callbacks for splash screens.</para>
    /// <para><see cref="SplashScreen"/> and <see cref="LoadingScreen"/> cannot coexist within the same scene.</para>
    /// </summary>
    public abstract class SplashScreen : LoadingScreen
    {

#pragma warning disable CS0414
        [SerializeField, HideInInspector] private bool isSplashScreen = true;
#pragma warning restore CS0414

        /// <summary>
        /// <para>Called when scene manager is ready to display the splash screen.</para>
        /// <para>Example: yielding new WaitForSeconds(5) will show the splash screen for 5 seconds.</para>
        /// </summary>
        public abstract IEnumerator DisplaySplashScreen();

        public override IEnumerator OnOpen(ISceneOperation operation)
        { yield break; }

        public override IEnumerator OnClose(ISceneOperation operation)
        { yield return DisplaySplashScreen(); }

    }

}
