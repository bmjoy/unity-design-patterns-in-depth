#pragma warning disable CS0414 //The field is assigned but not used
#pragma warning disable IDE0052 // Remove unread private members
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using Lazy.Utility;
using System.Collections;
using UnityEngine;

public class PressAnyButtonLoadingScreen : LoadingScreen
{
    /**
     * This is best used with 
     * if (AdvancedSceneManager.Utility.LoadingScreenUtility.IsAnyLoadingScreenOpen) { }
     * so you can start the game after loading screen is closed
     */
    bool pressed;
    bool canPress;

    public override IEnumerator OnOpen(ISceneOperation operation)
    {
        // In this example there is no fades, so we are just going to get going right away
        yield break;
    }
    public override IEnumerator OnClose(ISceneOperation operation)
    {
        // We dont want it to activate before it's loaded
        canPress = true;
        // Unity's corouting doesnt work here, apply our. 
        yield return WaitUntil().StartCoroutine();
    }


    private void Update()
    {
#if (ENABLE_LEGACY_INPUT_MANAGER)
        if (Input.anyKey && canPress)
        {
            pressed = true;
        }
#else
        if (UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame) pressed = true;
#endif
    }

    private IEnumerator WaitUntil()
    {
        yield return new WaitUntil(() => pressed);
    }
}
