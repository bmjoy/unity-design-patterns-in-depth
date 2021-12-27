using AdvancedSceneManager;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
#endif

[AddComponentMenu("")]
public class DefaultSplashScreen : AdvancedSceneManager.Callbacks.SplashScreen
{

    public CanvasGroup group;
    public Image image;
    public float fadeDuration = 1;
    public float waitDuration = 2.5f;

    void Start()
    {
        group.alpha = 0;
        //Use same color as previous splash screen, if enabled, defaults to black otherwise
        image.color = SceneManager.settings.buildUnitySplashScreenColor;
    }
    void Update()
    {
        //Skip splash screen when any of the buttons are pressed
        if (IsSkipButtonPressed())
            coroutine?.Stop();
    }

    bool IsSkipButtonPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (UnityEngine.InputSystem.Keyboard.current?.spaceKey?.isPressed ?? false) || (UnityEngine.InputSystem.Keyboard.current?.escapeKey?.isPressed ?? false) || (UnityEngine.InputSystem.Mouse.current?.leftButton?.isPressed ?? false) ||
            (UnityEngine.InputSystem.Gamepad.current?.aButton?.isPressed ?? false);
#else
        return UnityEngine.Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButton(0);
#endif
    }

    GlobalCoroutine coroutine;

    public override IEnumerator DisplaySplashScreen()
    {
        //Run the coroutines in a 'chain', this makes it easier to cancel splash screen on skip button
        coroutine = CoroutineUtility.Chain(
            () => group.Fade(1, fadeDuration),
            () => new WaitForSecondsRealtime(waitDuration),
            () => group.Fade(0, fadeDuration));

        yield return coroutine;
        group.alpha = 0;

    }

}
