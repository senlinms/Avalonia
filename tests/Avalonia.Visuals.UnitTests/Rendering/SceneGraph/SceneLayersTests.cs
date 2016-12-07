using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class SceneLayersTests
    {
        [Fact]
        public void Layers_Should_Be_Ordered()
        {
            Border border;
            Decorator decorator;
            var root = new TestRoot
            {
                Child = border = new Border
                {
                    Child = decorator = new Decorator(),
                }
            };

            var target = new SceneLayers(root);
            target.Add(decorator);
            target.Add(border);

            var result = target.Select(x => x.LayerRoot).ToArray();

            Assert.Equal(new[] { root, border, decorator }, result);
        }
    }
}
