using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class SceneLayers : IEnumerable<SceneLayer>
    {
        private List<SceneLayer> _inner = new List<SceneLayer>();
        private Dictionary<IVisual, SceneLayer> _index = new Dictionary<IVisual, SceneLayer>();

        public SceneLayers(IVisual root)
        {
            LayerRoot = root;
            Add(root);
        }

        public bool HasDirty
        {
            get
            {
                foreach (var layer in _inner)
                {
                    if (!layer.Dirty.IsEmpty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public IVisual LayerRoot { get; }
        public SceneLayer this[int index] => _inner[index];
        public SceneLayer this[IVisual visual] => _index[visual];

        public SceneLayer Add(IVisual layerRoot)
        {
            var layer = new SceneLayer(layerRoot);
            _index.Add(layerRoot, layer);
            _inner.Add(layer);
            return layer;
        }

        public SceneLayers Clone()
        {
            var result = new SceneLayers(LayerRoot);

            foreach (var src in _inner)
            {
                if (src.LayerRoot != LayerRoot)
                {
                    var dest = src.Clone();
                    result._index.Add(dest.LayerRoot, dest);
                    result._inner.Add(dest);
                }
            }

            return result;
        }

        public IEnumerator<SceneLayer> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
