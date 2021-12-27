using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using Lazy.Utility;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Quotes;

public class QuoteLoadingScreen : LoadingScreen
{
    public Quotes quotes;
    public Image Background;
    public Image fade;
    public Text Quote;
    public Text Name;

    public float fadeDuration = 0.5f;

    public override IEnumerator OnOpen(ISceneOperation operation)
    {
        Quote quote = quotes.quoteList[UnityEngine.Random.Range(0, quotes.quoteList.Count - 1)];
        Quote.text = quote.quote;
        Name.text = quote.name;

        yield return LerpFloat(f =>
        {
            SetAlpha(f);
        }, 1, 0, fadeDuration).StartCoroutine();
    }
    public override IEnumerator OnClose(ISceneOperation operation)
    {

        yield return LerpFloat(f =>
        {
            SetAlpha(f);
        }, 0, 1, fadeDuration, (v) =>
        {
            Background.enabled = false;
            Quote.enabled = false;
            Name.enabled = false;
        }).StartCoroutine();

        yield return LerpFloat(f =>
        {
            SetAlpha(f);
        }, 1, 0, fadeDuration).StartCoroutine();
    }

    private void SetAlpha(float f)
    {
        Color tempColor = fade.color;
        tempColor.a = f;
        fade.color = tempColor;
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
