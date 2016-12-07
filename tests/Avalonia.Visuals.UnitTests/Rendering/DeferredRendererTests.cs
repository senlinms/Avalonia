﻿using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    public class DeferredRendererTests
    {
        [Fact]
        public void First_Frame_Calls_UpdateScene_On_Dispatcher()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var dispatcher = new Mock<IDispatcher>();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: null,
                dispatcher: dispatcher.Object);

            RunFrame(loop);

            dispatcher.Verify(x => 
                x.InvokeAsync(
                    It.Is<Action>(a => a.Method.Name == "UpdateScene"),
                    DispatcherPriority.Render));
        }

        [Fact]
        public void First_Frame_Calls_SceneBuilder_UpdateAll()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = new Mock<ISceneBuilder>();
            var dispatcher = new ImmediateDispatcher();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            RunFrame(loop);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()));
        }

        [Fact]
        public void Frame_Does_Not_Call_SceneBuilder_If_No_Dirty_Controls()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = new Mock<ISceneBuilder>();
            var dispatcher = new ImmediateDispatcher();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            IgnoreFirstFrame(loop, sceneBuilder);
            RunFrame(loop);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()), Times.Never);
            sceneBuilder.Verify(x => x.Update(It.IsAny<Scene>(), It.IsAny<Visual>()), Times.Never);
        }

        [Fact]
        public void Frame_Should_Call_SceneBuilder_Update_With_Dirty_Controls()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = new Mock<ISceneBuilder>();
            var dispatcher = new ImmediateDispatcher();
            var control1 = new Border();
            var control2 = new Canvas();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            IgnoreFirstFrame(loop, sceneBuilder);
            target.AddDirty(control1);
            target.AddDirty(control2);
            RunFrame(loop);

            sceneBuilder.Verify(x => x.Update(It.IsAny<Scene>(), control1));
            sceneBuilder.Verify(x => x.Update(It.IsAny<Scene>(), control2));
        }

        [Fact]
        public void Frame_Should_Create_Layer_For_Root()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var rootLayer = new Mock<IRenderTargetBitmapImpl>();
            var dispatcher = new ImmediateDispatcher();

            var sceneBuilder = new Mock<ISceneBuilder>();
            sceneBuilder.Setup(x => x.UpdateAll(It.IsAny<Scene>()))
                .Callback<Scene>(scene =>
                {
                    ////var rects = new DirtyRects();
                    ////rects.Add(new Rect(root.ClientSize));
                    ////dirty.Add(root, rects);
                });

            var layers = new Mock<IRenderLayerFactory>();
            layers.Setup(x => x.CreateLayer(root, root.ClientSize)).Returns(CreateLayer());

            var renderInterface = new Mock<IPlatformRenderInterface>();

            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                layerFactory: layers.Object,
                dispatcher: dispatcher);

            RunFrame(loop);

            layers.Verify(x => x.CreateLayer(root, root.ClientSize));
        }

        [Fact]
        public void Should_Create_And_Delete_Layers_For_Transparent_Controls()
        {
            Border border;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = border = new Border
                    {
                        Background = Brushes.Green,
                    }
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var loop = new Mock<IRenderLoop>();
            var layerFactory = new MockRenderLayerFactory(new Dictionary<IVisual, IRenderTargetBitmapImpl>
            {
                { root, CreateLayer() },
                { border, CreateLayer() },
            });

            var target = new DeferredRenderer(
                root, 
                loop.Object,
                layerFactory: layerFactory,
                dispatcher: new ImmediateDispatcher());
            root.Renderer = target;

            RunFrame(loop);

            var rootContext = layerFactory.GetMockDrawingContext(root);
            var borderContext = layerFactory.GetMockDrawingContext(border);

            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);
            borderContext.Verify(x => x.FillRectangle(It.IsAny<IBrush>(), It.IsAny<Rect>(), It.IsAny<float>()), Times.Never);

            rootContext.ResetCalls();
            borderContext.ResetCalls();
            border.Opacity = 0.5;
            RunFrame(loop);

            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Never);
            borderContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);

            rootContext.ResetCalls();
            borderContext.ResetCalls();
            border.Opacity = 1;
            RunFrame(loop);

            layerFactory.GetMockBitmap(border).Verify(x => x.Dispose());
            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);
            borderContext.Verify(x => x.FillRectangle(It.IsAny<IBrush>(), It.IsAny<Rect>(), It.IsAny<float>()), Times.Never);
        }

        private void IgnoreFirstFrame(Mock<IRenderLoop> loop, Mock<ISceneBuilder> sceneBuilder)
        {
            RunFrame(loop);
            sceneBuilder.ResetCalls();
        }

        private void RunFrame(Mock<IRenderLoop> loop)
        {
            loop.Raise(x => x.Tick += null, EventArgs.Empty);
        }

        private IRenderTargetBitmapImpl CreateLayer()
        {
            return Mock.Of<IRenderTargetBitmapImpl>(x =>
                x.CreateDrawingContext() == Mock.Of<IDrawingContextImpl>());
        }

        private class MockRenderLayerFactory : IRenderLayerFactory
        {
            private IDictionary<IVisual, IRenderTargetBitmapImpl> _layers;

            public MockRenderLayerFactory(IDictionary<IVisual, IRenderTargetBitmapImpl> layers)
            {
                _layers = layers;
            }

            public IRenderTargetBitmapImpl CreateLayer(IVisual layerRoot, Size size)
            {
                return _layers[layerRoot];
            }

            public Mock<IRenderTargetBitmapImpl> GetMockBitmap(IVisual layerRoot)
            {
                return Mock.Get(_layers[layerRoot]);
            }

            public Mock<IDrawingContextImpl> GetMockDrawingContext(IVisual layerRoot)
            {
                return Mock.Get(_layers[layerRoot].CreateDrawingContext());
            }
        }
    }
}
