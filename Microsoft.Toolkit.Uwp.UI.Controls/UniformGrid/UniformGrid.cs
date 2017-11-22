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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// The UniformGrid control presents information within a Grid with even spacing.
    /// </summary>
    public partial class UniformGrid : Grid
    {
        private static void Fill(ref bool[,] array, int row, int col, int width, int height)
        {
            for (int r = row; r < row + width; r++)
            {
                for (int c = col; c < col + height; c++)
                {
                    if (r < array.GetLength(0) && c < array.GetLength(1))
                    {
                        array[r, c] = true;
                    }
                }
            }
        }

        #pragma warning disable SA1009 // Closing parenthesis must be followed by a space.
        private static IEnumerable<(int row, int column)> GetFreeSpot(bool[,] array, int? firstcolumn, bool reverse)
        #pragma warning restore SA1009 // Closing parenthesis must be followed by a space.
        {
            if (!reverse)
            {
                for (int r = 0; r < array.GetLength(0); r++)
                {
                    int start = (r == 0 && firstcolumn != null) ? firstcolumn.Value : 0;
                    for (int c = start; c < array.GetLength(1); c++)
                    {
                        if (!array[r, c])
                        {
                            yield return (r, c);
                        }
                    }
                }
            }
            else
            {
                for (int r = 0; r < array.GetLength(0); r++)
                {
                    int start = (r == 0 && firstcolumn != null) ? firstcolumn.Value : array.GetLength(1) - 1;
                    for (int c = start; c >= 0; c--)
                    {
                        if (!array[r, c])
                        {
                            yield return (r, c);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Measure the controls before layout.
        /// </summary>
        /// <param name="availableSize">Size available from parent.</param>
        /// <returns>Desired Size</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var dim = this.GetDimensions();

            /*Size childSize = new Size(
                (availableSize.Width - (ColumnSpacing * (dim.columns - 1))) / dim.columns,
                (availableSize.Height - (RowSpacing * (dim.rows - 1))) / dim.rows);*/

            // Mark existing definitions so we don't erase them.
            foreach (var rd in RowDefinitions)
            {
                if (GetAutoLayout(rd) == null)
                {
                    SetAutoLayout(rd, false);
                }
            }

            foreach (var cd in ColumnDefinitions)
            {
                if (GetAutoLayout(cd) == null)
                {
                    SetAutoLayout(cd, false);
                }
            }

            // Remove non-autolayout rows we've added and then add them in the right spots.
            if (dim.rows != this.RowDefinitions.Count)
            {
                for (int r = this.RowDefinitions.Count - 1; r >= 0; r--)
                {
                    if (GetAutoLayout(this.RowDefinitions[r]) == true)
                    {
                        this.RowDefinitions.RemoveAt(r);
                    }
                }

                for (int r = 0; r < dim.rows; r++)
                {
                    if (!(this.RowDefinitions.Count >= r + 1 && GetRowDefinitionRow(this.RowDefinitions[r]) == r))
                    {
                        var rd = new RowDefinition();
                        SetAutoLayout(rd, true);
                        this.RowDefinitions.Insert(r, rd);
                    }
                }
            }

            // Remove non-autolayout columns we've added and then add them in the right spots.
            if (dim.columns != this.ColumnDefinitions.Count)
            {
                for (int c = this.ColumnDefinitions.Count - 1; c >= 0; c--)
                {
                    if (GetAutoLayout(this.ColumnDefinitions[c]) == true)
                    {
                        this.ColumnDefinitions.RemoveAt(c);
                    }
                }

                for (int c = 0; c < dim.columns; c++)
                {
                    if (!(this.ColumnDefinitions.Count >= c + 1 && GetColumnDefinitionColumn(this.ColumnDefinitions[c]) == c))
                    {
                        var cd = new ColumnDefinition();
                        SetAutoLayout(cd, true);
                        this.ColumnDefinitions.Insert(c, cd);
                    }
                }
            }

            // Get all Visible FrameworkElement Children
            var visible = this.Children.Where(item => item.Visibility != Visibility.Collapsed && item is FrameworkElement).Select(item => item as FrameworkElement);

            bool[,] spots = new bool[dim.rows, dim.columns];

            // Figure out which children we should automatically layout and where available openings are.
            foreach (var child in visible)
            {
                var row = GetRow(child);
                var col = GetColumn(child);
                var rowspan = GetRowSpan(child);
                var colspan = GetColumnSpan(child);

                // TODO: Document
                // If an element needs to be forced in the 0, 0 position, they should manually set UniformGrid.AutoLayout to False for that element.
                if (row == 0 && col == 0 && GetAutoLayout(child) == null)
                {
                    SetAutoLayout(child, true);
                }
                else
                {
                    SetAutoLayout(child, false);
                    Fill(ref spots, row, col, rowspan, colspan);
                }
            }

            // Set Grid Row/Col for every child with autolayout = true
            // Backwards with FlowDirection
            var freespots = GetFreeSpot(spots, this.FirstColumn, this.FlowDirection == FlowDirection.RightToLeft).GetEnumerator();
            foreach (var child in visible)
            {
                if (GetAutoLayout(child) == true)
                {
                    if (freespots.MoveNext())
                    {
                        var loc = freespots.Current;

                        SetRow(child, loc.row);
                        SetColumn(child, loc.column);
                    }
                    else
                    {
                        // TODO: We've run out of spots somehow? Now What?
                    }
                }
            }

            // Perform regular grid layout now.
            return base.MeasureOverride(availableSize);
        }

        #pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
        private (int rows, int columns) GetDimensions()
        #pragma warning restore SA1008 // Opening parenthesis must be spaced correctly
        {
            int rows = this.Rows;
            int cols = this.Columns;

            if (rows == 0 || cols == 0)
            {
                // TODO: Cache as needed above
                var children = this.Children.Where(item => item.Visibility != Visibility.Collapsed && item is FrameworkElement).Select(item => item as FrameworkElement);

                // Calculate the size of all objects in the grid to know how much space we need.
                // TODO: Need to trim size of objects that go out of bounds?
                var count = children.Sum(item => GetRowSpan(item) * GetColumnSpan(item));

                if (rows == 0)
                {
                    if (cols > 0)
                    {
                        // TODO: Handle RightToLeft
                        var first = Math.Min(this.FirstColumn.HasValue ? this.FirstColumn.Value : 0, cols - 1);

                        // If we have columns but no rows, calculate rows based on column offset and number of children.
                        rows = (count + first + (cols - 1)) / cols;
                        return (rows, cols);
                    }

                    // Otherwise, determine square layout if both are zero.
                    rows = (int)Math.Ceiling(Math.Sqrt(count));
                    return (rows, rows);
                }
                else if (cols == 0)
                {
                    // If we have rows and no columns, then calculate columns needed based on rows
                    // TODO: Do we need to account for FirstColumn here too?
                    cols = (count + (rows - 1)) / rows;
                }
            }

            return (rows, cols);
        }
    }
}
