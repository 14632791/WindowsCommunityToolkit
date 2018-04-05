﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Graphics.Canvas;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Parsers.Markdown;
using Microsoft.Toolkit.Parsers.Markdown.Blocks;
using Microsoft.Toolkit.Parsers.Markdown.Enums;
using Microsoft.Toolkit.Parsers.Markdown.Inlines;
using Microsoft.Toolkit.Parsers.Markdown.Render;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// Infinite Canvas
    /// </summary>
    public partial class InfiniteCanvas : Control
    {
        InkCanvas _inkCanvas;
        VirtualDrawingSurface _canvasOne;
        IReadOnlyList<InkStroke> wetInkStrokes;
        InkSynchronizer inkSync;

        internal const float LargeCanvasWidthHeight = 1 << 21;

        public InfiniteCanvas()
        {
            this.DefaultStyleKey = typeof(InfiniteCanvas);
        }

        private InkToolbarCustomToolButton _enableTextButton;
        private InfiniteCanvasTextBox _canvasTextBox;
        private StackPanel _canvasTextBoxTools;
        protected override void OnApplyTemplate()
        {
            _canvasTextBoxTools = (StackPanel)GetTemplateChild("CanvasTextBoxTools");

            _canvasOne = (VirtualDrawingSurface)GetTemplateChild("canvasOne");
            OutputGrid = (Canvas)GetTemplateChild("OutputGrid");

            inkScrollViewer = (ScrollViewer)GetTemplateChild("inkScrollViewer");
            var eraseAllButton = (InkToolbarCustomToolButton)GetTemplateChild("EraseAllButton");

            _canvasTextBox = (InfiniteCanvasTextBox)GetTemplateChild("CanvasTextBox");

            _enableTextButton = (InkToolbarCustomToolButton)GetTemplateChild("EnableTextButton");

            _enableTextButton.Checked += _enableTextButton_Checked;
            _enableTextButton.Unchecked += _enableTextButton_Unchecked;
            eraseAllButton.Click += EraseAllButton_Click;

            canToolBar = (InkToolbar)GetTemplateChild("canToolBar");

            _inkCanvas = (InkCanvas)GetTemplateChild("inkCanvas");
            inkScrollViewer.PointerPressed += InkScrollViewer_PointerPressed;
            _canvasTextBox.TextChanged += _canvasTextBox_TextChanged;

            MainPage_Loaded();
            base.OnApplyTemplate();
        }

        public float FontSize { get; set; } = 22;

        private TextDrawable _selectedTextDrawable;

        private void _canvasTextBox_TextChanged(object sender, string text)
        {
            if (string.IsNullOrEmpty(text) && _selectedTextDrawable == null)
            {
                return;
            }

            if (_selectedTextDrawable != null)
            {
                if (string.IsNullOrEmpty(text))
                {
                    _canvasOne.RemoveDrawable(_selectedTextDrawable);
                    _selectedTextDrawable = null;
                    _canvasOne.ReDraw(ViewPort);
                }
                else
                {
                    _selectedTextDrawable.Text = text;
                }

                _canvasOne.ReDraw(ViewPort);
                return;
            }

            _selectedTextDrawable = new TextDrawable(
                _lastInputPoint.Y, _lastInputPoint.X,
                FontSize,
                _canvasTextBox.GetEditZoneHeight(),
                _canvasTextBox.GetEditZoneWidth(),
                text);

            _canvasOne.AddDrawable(_selectedTextDrawable);
            _canvasOne.ReDraw(ViewPort);
        }

        Point _lastInputPoint;

        private void DisableScrollView()
        {
            inkScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
            inkScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
        }

        private void EnableScrollView()
        {
            inkScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
            inkScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
        }

        private void InkScrollViewer_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_enableTextButton.IsChecked ?? false)
            {

                var point = e.GetCurrentPoint(inkScrollViewer);
                _lastInputPoint = new Point((point.Position.X + inkScrollViewer.HorizontalOffset) / inkScrollViewer.ZoomFactor, (point.Position.Y + inkScrollViewer.VerticalOffset) / inkScrollViewer.ZoomFactor);

                var currentTextDrawable = _canvasOne.GetEditableTextDrawable(_lastInputPoint, ViewPort);

                if (currentTextDrawable != null)
                {
                    _canvasTextBox.Visibility = Visibility.Visible;
                    _canvasTextBox.SetText(currentTextDrawable.Text);

                    // ToDO create a cahced value for fontsize/2
                    Canvas.SetLeft(_canvasTextBox, currentTextDrawable.Bounds.X - (FontSize / 2));

                    Canvas.SetTop(_canvasTextBox, currentTextDrawable.Bounds.Y - 2);
                    _selectedTextDrawable = currentTextDrawable;

                    DisableScrollView();
                    return;
                }

                _inkCanvas.Visibility = Visibility.Collapsed;
                DisableScrollView();
                ClearTextBoxValue();
                _canvasTextBox.Visibility = Visibility.Visible;
                Canvas.SetLeft(_canvasTextBox, _lastInputPoint.X - (FontSize / 2));
                Canvas.SetTop(_canvasTextBox, _lastInputPoint.Y - 2);
            }
        }

        private void _enableTextButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _canvasTextBox.Visibility = Visibility.Collapsed;
            EnableScrollView();
            _inkCanvas.Visibility = Visibility.Visible;
        }

        private void _enableTextButton_Checked(object sender, RoutedEventArgs e)
        {
            _inkCanvas.Visibility = Visibility.Collapsed;
        }

        private void EraseAllButton_Click(object sender, RoutedEventArgs e)
        {
            _canvasOne.ClearAll(ViewPort);
        }

        public InkToolbar canToolBar { get; set; }

        public Canvas OutputGrid { get; set; }
        public ScrollViewer inkScrollViewer { get; set; }

        private void MainPage_Loaded()
        {
            _inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;

            this.inkSync = this._inkCanvas.InkPresenter.ActivateCustomDrying();
            this._inkCanvas.InkPresenter.StrokesCollected += OnStrokesCollected;
            this._inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;


            inkScrollViewer.MaxZoomFactor = 4.0f;
            inkScrollViewer.MinZoomFactor = 0.25f;
            inkScrollViewer.ViewChanged += InkScrollViewer_ViewChanged;
            inkScrollViewer.SizeChanged += InkScrollViewer_SizeChanged;

            OutputGrid.Width = LargeCanvasWidthHeight;
            OutputGrid.Height = LargeCanvasWidthHeight;
            _inkCanvas.Width = LargeCanvasWidthHeight;
            _inkCanvas.Height = LargeCanvasWidthHeight;
            _canvasOne.Width = LargeCanvasWidthHeight;
            _canvasOne.Height = LargeCanvasWidthHeight;

            Application.Current.LeavingBackground += Current_LeavingBackground;

            Canvas.SetLeft(_canvasTextBox, 0);
            Canvas.SetTop(_canvasTextBox, 0);

            _canvasTextBox.FontSize = FontSize;
        }

        private async void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            // work around to virtual drawing surface bug.
            await Task.Delay(1000);
            _canvasOne.ReDraw(ViewPort);
        }


        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (canToolBar.ActiveTool == canToolBar.GetToolButton(InkToolbarTool.Eraser))
            {
                _canvasOne.Erase(args.CurrentPoint.Position, ViewPort);
            }
        }

        private void ReDrawCanvas()
        {
            _canvasOne.ReDraw(ViewPort);
        }

        private void InkScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClearTextBoxValue();
            ReDrawCanvas();
        }

        void OnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            IReadOnlyList<InkStroke> strokes = this.inkSync.BeginDry();
            var inkDrawable = new InkDrawable(strokes);
            _canvasOne.AddDrawable(inkDrawable);
            this.inkSync.EndDry();

            ReDrawCanvas();
        }

        private void InkScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate)
            {
                _canvasTextBox.Visibility = Visibility.Collapsed;
                ClearTextBoxValue();
                _canvasOne.UpdateZoomFactor(inkScrollViewer.ZoomFactor);
                ReDrawCanvas();
            }
        }

        private void ClearTextBoxValue()
        {
            EnableScrollView();
            _selectedTextDrawable = null;
            _canvasTextBox.Clear();
        }

        private Rect ViewPort => new Rect(inkScrollViewer.HorizontalOffset / inkScrollViewer.ZoomFactor, inkScrollViewer.VerticalOffset / inkScrollViewer.ZoomFactor, inkScrollViewer.ViewportWidth / inkScrollViewer.ZoomFactor, inkScrollViewer.ViewportHeight / inkScrollViewer.ZoomFactor);
    }
}
