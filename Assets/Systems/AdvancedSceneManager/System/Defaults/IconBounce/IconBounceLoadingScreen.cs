using AdvancedSceneManager.Core;
using Lazy.Utility;
using System;
using System.Collections;
using UnityEngine;

public class IconBounceLoadingScreen : AdvancedSceneManager.Callbacks.LoadingScreen
{
    public Vector2 IconStartSize = new Vector2(5000, 5000);
    public float IconStartRotationZ = -50f;
    public float duration = 1.4f;

    public RectTransform IconTransform;
    public RectTransform BackgroundTransform;

    public override IEnumerator OnOpen(ISceneOperation operation)
    {
        yield return LerpFloat(f =>
        {
            AnimateTick(f);
        }, 0, 1, duration).StartCoroutine();
    }

    public override IEnumerator OnClose(ISceneOperation operation)
    {
        yield return LerpFloat(f =>
        {
            AnimateTick(f);
        }, 1, 0, duration).StartCoroutine();
    }

    void AnimateTick(float t)
    {
        Vector2 iconSize = Vector2.Lerp(IconStartSize, new Vector2(0, 0), t);
        IconTransform.sizeDelta = iconSize;

        float rotation = Mathf.Lerp(IconStartRotationZ, 0, t);
        IconTransform.rotation = Quaternion.Euler(0, 0, rotation);

        BackgroundTransform.rotation = Quaternion.Euler(0, 0, 0);
    }

    IEnumerator LerpFloat(Action<float> @return, float from, float to, float duration = 2, Action<bool> Callback = null)
    {
        var i = 0f;
        var rate = 1f / duration;

        while (i < 1f)
        {
            i += Time.deltaTime * rate;
            @return(Mathf.Lerp(from, to, Mathf.SmoothStep(0.0f, 1.0f, i)));
            yield return null;
        }
        @return(to);
        Callback?.Invoke(true);
    }

}
