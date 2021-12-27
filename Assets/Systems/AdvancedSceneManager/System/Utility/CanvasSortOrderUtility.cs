using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility class to manage sort order on canvases.</summary>
    public static class CanvasSortOrderUtility
    {

        static readonly Dictionary<Canvas, (Canvas above, Canvas below)> canvases = new Dictionary<Canvas, (Canvas above, Canvas below)>();

        /// <summary>Removes this canvas from the managed list.</summary>
        public static void Remove(Canvas canvas)
        {
            if (canvas)
                canvases.Remove(canvas);
        }

        /// <summary>Sets the sort order on this canvas to be on top of all other canvases managed by <see cref="CanvasSortOrderUtility"/>.</summary>
        public static void PutOnTop(this Canvas canvas)
        {

            if (!canvas)
                return;

            if (!canvases.ContainsKey(canvas))
                canvases.Add(canvas, default);

            SetOrder(GetPreferredOrder(top: canvas));

        }

        /// <summary>Sets the sort order on this canvas to be on bottom of all other canvases managed by <see cref="CanvasSortOrderUtility"/>.</summary>
        public static void PutAtBottom(this Canvas canvas)
        {

            if (!canvas)
                return;

            if (!canvases.ContainsKey(canvas))
                canvases.Add(canvas, default);

            SetOrder(GetPreferredOrder(bottom: canvas));

        }

        /// <summary>
        /// <para>Adds a contraint on the sort order of this <see cref="Canvas"/> based on one or two other canvases.</para>
        /// <para>See parameter comments for more info.</para>
        /// </summary>
        /// <param name="above">Makes sure that this canvas is always above this one.</param>
        /// <param name="below">Makes sure that this canvas is always below this one.</param>
        public static void MakeSure(this Canvas canvas, Canvas above = null, Canvas below = null)
        {

            if (!canvas)
                return;

            if (above == below ||
                canvas == above || canvas == below)
                throw new ArgumentException("Above and below cannot be the same, and canvas can not be the same as above or below.");

            if (!canvases.ContainsKey(canvas))
                canvases.Add(canvas, (above, below));
            else if (above)
                canvases[canvas] = (above, canvases[canvas].below);
            else if (above)
                canvases[canvas] = (canvases[canvas].above, below);
        
            SetOrder(GetPreferredOrder());
        
        }

        static Canvas[] GetPreferredOrder(Canvas top = null, Canvas bottom = null)
        {

            var canvases = CanvasSortOrderUtility.canvases.Select(c => c.Key).Where(c => c).OrderBy(c => c.sortingOrder).ToList();
            
            //Set top and bottom
            if (top)
            {
                canvases.Remove(top);
                canvases.Add(top);
            }
            else if (bottom)
            {
                canvases.Remove(bottom);
                canvases.Add(bottom);
            }

            //Set contraints
            return canvases.
                Select(c => (canvas: c, constraints: CanvasSortOrderUtility.canvases[c])).
                OrderBy(c => canvases.IndexOf(c.constraints.above) > canvases.IndexOf(c.canvas)).
                ThenBy(c => canvases.IndexOf(c.constraints.below) < canvases.IndexOf(c.canvas)).
                Select(c => c.canvas).ToArray();

        }

        static void SetOrder(Canvas[] canvases)
        {

            var startValue = short.MaxValue - canvases.Length - 200;
            for (int i = 0; i < canvases.Length; i++)
                canvases[i].sortingOrder = startValue + i;

        }

    }

}
