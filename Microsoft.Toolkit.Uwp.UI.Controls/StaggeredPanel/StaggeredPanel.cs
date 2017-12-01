﻿using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    public class StaggeredPanel : Panel
    {
        private double _columnWidth;

        public double DesiredColumnWidth
        {
            get { return (double)GetValue(DesiredColumnWidthProperty); }
            set { SetValue(DesiredColumnWidthProperty, value); }
        }

        public static readonly DependencyProperty DesiredColumnWidthProperty = DependencyProperty.Register(
            nameof(DesiredColumnWidth),
            typeof(double),
            typeof(StaggeredPanel),
            new PropertyMetadata(250d, OnDesiredColumnWidthChanged));

        protected override Size MeasureOverride(Size availableSize)
        {
            _columnWidth = DesiredColumnWidth;
            int numColumns = (int)Math.Floor(availableSize.Width / _columnWidth);
            var columnHeights = new double[numColumns];

            for (int i = 0; i < Children.Count; i++)
            {
                var columnIndex = GetColumnIndex(columnHeights);

                var child = Children[i];
                child.Measure(new Size(_columnWidth, availableSize.Height));
                var elementSize = child.DesiredSize;
                columnHeights[columnIndex] += elementSize.Height;
            }

            double desiredHeight = columnHeights.Max();

            return new Size(availableSize.Width, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double offset = 0;
            int numColumns = (int)Math.Floor(finalSize.Width / _columnWidth);
            var columnHeights = new double[numColumns];

            for (int i = 0; i < Children.Count; i++)
            {
                var columnIndex = GetColumnIndex(columnHeights);

                var child = Children[i];
                var elementSize = child.DesiredSize;

                double elementWidth = elementSize.Width;
                double elementHeight = elementSize.Height;
                if (elementWidth > _columnWidth)
                {
                    double differencePercentage = _columnWidth / elementWidth;
                    elementHeight = elementHeight * differencePercentage;
                    elementWidth = _columnWidth;
                }

                Rect bounds = new Rect(offset + (_columnWidth * columnIndex), columnHeights[columnIndex], elementWidth, elementHeight);
                child.Arrange(bounds);

                columnHeights[columnIndex] += elementSize.Height;
            }

            return base.ArrangeOverride(finalSize);
        }

        private static void OnDesiredColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StaggeredPanel panel)
            {
                panel.InvalidateMeasure();
            }
        }

        private int GetColumnIndex(double[] columnHeights)
        {
            int columnIndex = 0;
            double height = columnHeights[0];
            for (int j = 1; j < columnHeights.Length; j++)
            {
                if (columnHeights[j] < height)
                {
                    columnIndex = j;
                    height = columnHeights[j];
                }
            }

            return columnIndex;
        }
    }
}