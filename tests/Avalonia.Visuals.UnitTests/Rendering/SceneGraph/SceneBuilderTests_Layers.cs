﻿using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Layout;
using Avalonia.Rendering;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public partial class SceneBuilderTests
    {
        [Fact]
        public void Control_With_Transparency_Should_Start_New_Layer()
        {
            using (TestApplication())
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Opacity = 0.5,
                            Background = Brushes.Red,
                            Padding = new Thickness(12),
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                var layout = AvaloniaLocator.Current.GetService<ILayoutManager>();
                layout.ExecuteInitialLayoutPass(tree);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                var rootNode = (VisualNode)scene.Root;
                var borderNode = (VisualNode)scene.FindNode(border);
                var canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(border, borderNode.LayerRoot);
                Assert.Same(border, canvasNode.LayerRoot);

                Assert.Equal(2, scene.Layers.Count());
                Assert.Empty(scene.Layers.Select(x => x.LayerRoot).Except(new IVisual[] { tree, border }));

                border.Opacity = 1;
                scene = scene.Clone();

                sceneBuilder.Update(scene, border);

                rootNode = (VisualNode)scene.Root;
                borderNode = (VisualNode)scene.FindNode(border);
                canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(tree, borderNode.LayerRoot);
                Assert.Same(tree, canvasNode.LayerRoot);

                var rootDirty = scene.Layers[tree].Dirty;
                var borderDirty = scene.Layers[border].Dirty;

                Assert.Equal(1, rootDirty.Count());
                Assert.Equal(1, borderDirty.Count());
                Assert.Equal(new Rect(21, 21, 58, 78), rootDirty.Single());
                Assert.Equal(new Rect(21, 21, 58, 78), borderDirty.Single());
            }
        }
    }
}
