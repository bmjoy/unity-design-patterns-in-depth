using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>
    /// Performs startup sequence, see <see cref="Runtime.Start"/>:
    /// <para><see cref="FadeOut(float, Color?)(float)"/>.</para>
    /// <para><see cref="SetProfileInBuild"/>.</para>
    /// <para><see cref="ReloadAssets"/>.</para>
    /// <para><see cref="CloseAllUnityScenesAction"/>.</para>
    /// <para><see cref="PlaySplashScreenAction"/> (+ fade in).</para>
    /// <para><see cref="ShowStartupLoadingScreen(Scene)"/>.</para>
    /// <para><see cref="OpenCollectionsAndScenesFlaggedToOpenAtStartAction"/>.</para>
    /// <para><see cref="HideStartupLoadingScreen"/>.</para>
    /// </summary>
    public class StartupAction : AggregateAction
    {

        ///<inheritdoc cref="StartupAction"/>
        /// <param name="fadeColor">Defaults to unity splash screen color.</param>
        public StartupAction(bool skipSplashScreen = false, Color? fadeColor = null, float initialFadeDuration = 0, float beforeSplashScreenFadeDuration = 0.5f, SceneCollection collection = null, bool ignoreDoNotOpen = false) :
            base(

                skipSplashScreen ? null : new CallbackAction(() => FadeOut(initialFadeDuration, fadeColor)),
                new CloseAllUnityScenesAction(),
                skipSplashScreen ? null : new PlaySplashScreenAction(() => FadeIn(beforeSplashScreenFadeDuration)),

                new CallbackAction(ShowStartupLoadingScreen),
                new OpenCollectionsAndScenesFlaggedToOpenAtStartAction(collection, ignoreDoNotOpen),
                new CallbackAction(HideStartupLoadingScreen)

                )
        { }

        static SceneOperation<LoadingScreen> fade;
        static SceneOperation<LoadingScreen> loadingScreen;

        static IEnumerator FadeOut(float duration, Color? fadeColor)
        {
            if (Profile.current)
                yield return fade = LoadingScreenUtility.FadeOut(duration, color: fadeColor ?? SceneManager.settings.buildUnitySplashScreenColor);
            yield break;
        }

        static IEnumerator FadeIn(float duration) =>
            LoadingScreenUtility.FadeIn(fade?.value, duration);

        static IEnumerator ShowStartupLoadingScreen()
        {
            if (Profile.current)
            {
                yield return loadingScreen =
                    LoadingScreenUtility.OpenLoadingScreen(
                        Profile.current.startupLoadingScreen,
                        callbackBeforeBegin: l => l.operation = SceneManager.utility.currentOperation);
            }
            yield break;
        }

        static IEnumerator HideStartupLoadingScreen() =>
            LoadingScreenUtility.CloseLoadingScreen(loadingScreen?.value);

    }

}
