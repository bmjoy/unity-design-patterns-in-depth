using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
public class DefaultLoadingScreen : AdvancedSceneManager.Callbacks.LoadingScreen
{

    public CanvasGroup group;
    public Image image;
    public Slider slider;
    public float duration = 0.5f;
    public Color color;

    public override IEnumerator OnOpen(ISceneOperation operation)
    {
        image.color = color; //Color can be changed when using FadeUtility methods
        yield return group.Fade(1, duration);
    }

    public override IEnumerator OnClose(ISceneOperation operation)
    {
        //Hide slider before fade, since it is brighter than background and will 
        //appear to stay on screen for longer than background which looks bad
        if (slider)
            slider.gameObject.SetActive(false);
        yield return group.Fade(0, duration);
    }

    void Start()
    {
        group.alpha = 0;
    }

    void Update()
    {
        if (slider)
            slider.value = operation?.totalProgress ?? 1;
    }

}
