using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>Plays splash screen.</summary>
    public class PlaySplashScreenAction : SceneAction
    {

        public PlaySplashScreenAction(Func<IEnumerator> hideInitialLoadingScreen) =>
            this.hideInitialLoadingScreen = hideInitialLoadingScreen;

        readonly Func<IEnumerator> hideInitialLoadingScreen;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!Profile.current)
                yield break;

            if (Profile.current.splashScreen)
            {

                SceneOperation<LoadingScreen> async;
                yield return async = LoadingScreenUtility.OpenLoadingScreen(Profile.current.splashScreen).SetParent(SceneManager.utility.currentOperation);

                yield return hideInitialLoadingScreen?.Invoke();

                if (async.value)
                {

                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(async.value.gameObject.scene);
                    CreateCameraIfNeeded();

                    yield return LoadingScreenUtility.CloseLoadingScreen(async.value).SetParent(SceneManager.utility.currentOperation);

                }

            }

        }

        void CreateCameraIfNeeded()
        {

            if (Profile.current.createCameraForSplashScreen && !(Object.FindObjectOfType<Camera>() is Camera))
            {
                var obj = new GameObject("Camera");
                var camera = obj.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = SceneManager.settings.buildUnitySplashScreenColor;
            }

        }

    }

}
