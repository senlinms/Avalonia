using System;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class SceneLayer
    {
        public SceneLayer(IVisual layerRoot)
        {
            LayerRoot = layerRoot;
            Dirty = new DirtyRects();
            DistanceFromRoot = CalculateDistanceFromRoot(layerRoot);
        }

        public SceneLayer Clone()
        {
            return new SceneLayer(LayerRoot);
        }

        public IVisual LayerRoot { get; }
        public DirtyRects Dirty { get; }
        public int DistanceFromRoot { get; }

        private int CalculateDistanceFromRoot(IVisual visual)
        {
            var result = 0;

            while (!(visual is IRenderRoot))
            {
                visual = visual.VisualParent;

                if (visual == null)
                {
                    throw new AvaloniaInternalException(
                        "Attempted to create a SceneLayer for an unrooted visual.");
                }

                ++result;
            }

            return result;
        }
    }
}
