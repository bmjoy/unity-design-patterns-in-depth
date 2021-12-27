using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    public static class CanvasGroupExtensions
    {

        /// <summary>Animates the alpha of a <see cref="CanvasGroup"/>.</summary>
        public static IEnumerator Fade(this CanvasGroup group, float to, float duration, bool setBlocksRaycasts = true)
        {

            if (!group || !group.gameObject.activeInHierarchy)
                yield break;

            if (setBlocksRaycasts)
                group.blocksRaycasts = true;

            if (group.alpha == to)
                yield break;

            yield return LerpUtility.Lerp(group.alpha, to, duration, t =>
            {

                if (group)
                    group.alpha = t;

                if (setBlocksRaycasts)
                    group.blocksRaycasts = group.alpha > 0;

            });

        }

    }

}