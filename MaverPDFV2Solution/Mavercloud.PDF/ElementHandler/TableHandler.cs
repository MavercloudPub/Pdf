using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class TableHandler : ElementBaseHandler<Table>
    {
        private int? columnCount;

        private UnitValue[] columnWidth;

        private bool drawItems = true;


        public TableHandler(object sourceData, ElementItem item, string styleName, Document document) 
            : base(sourceData, item, styleName, document)
        {

        }

        public TableHandler(object sourceData, ElementItem item, string styleName, bool drawItems, Document document)
            : base(sourceData, item, styleName, document)
        {
            this.drawItems = drawItems;
        }

        public TableHandler(object sourceData, ElementItem item, string styleName, int? columnCount, Document document) 
            : base(sourceData, item, styleName, document)
        {
            this.columnCount = columnCount;
        }

        public TableHandler(object sourceData, ElementItem item, string styleName, int? columnCount, bool drawItems, Document document)
            : base(sourceData, item, styleName, document)
        {
            this.columnCount = columnCount;
            this.drawItems = drawItems;
        }

        public TableHandler(object sourceData, ElementItem item, string styleName, UnitValue[] columnWidth, Document document) 
            : base(sourceData, item, styleName, document)
        {
            this.columnWidth = columnWidth;
        }

        public TableHandler(object sourceData, ElementItem item, string styleName, UnitValue[] columnWidth, bool drawItems, Document document)
            : base(sourceData, item, styleName, document)
        {
            this.columnWidth = columnWidth;
            this.drawItems = drawItems;
        }


        public override Table Handle()
        {
            Table table = null;
            if (CheckStyle())
            {
                bool usePercentWidth = false;
                if (columnWidth == null)
                {
                    if (!string.IsNullOrEmpty(item.ColumnPercentWidth))
                    {
                        var widthArray = item.ColumnPercentWidth.Split(new char[] { ',' });
                        var percentArray = new List<float>();
                        foreach (var width in widthArray)
                        {
                            percentArray.Add(float.Parse(width));
                        }
                        this.columnWidth = UnitValue.CreatePercentArray(percentArray.ToArray());
                        usePercentWidth = true;
                    }
                    else if (!string.IsNullOrEmpty(item.ColumnPointWidth))
                    {
                        var widthArray = item.ColumnPointWidth.Split(new char[] { ',' });
                        var percentArray = new List<float>();
                        foreach (var width in widthArray)
                        {
                            percentArray.Add(float.Parse(width));
                        }
                        this.columnWidth = UnitValue.CreatePointArray(percentArray.ToArray());
                    }
                }
                if (this.columnWidth != null)
                {
                    if (item.LargeTable.GetValueOrDefault())
                    {
                        table = new Table(columnWidth, true);
                    }
                    else
                    {
                        table = new Table(columnWidth);
                    }
                    
                    table.SetFixedLayout();
                }              
                else
                {
                    if (this.columnCount.HasValue)
                    {
                        if (item.LargeTable.GetValueOrDefault())
                        {
                            table = new Table(this.columnCount.Value, true);
                        }
                        else
                        {
                            table = new Table(this.columnCount.Value);
                        }
                    }
                    else if (item.ColumnCount.HasValue)
                    {
                        if (item.LargeTable.GetValueOrDefault())
                        {
                            table = new Table(item.ColumnCount.Value, true);
                        }
                        else
                        {
                            table = new Table(item.ColumnCount.Value);
                        }
                        
                    }
                    else
                    {
                        if (item.LargeTable.GetValueOrDefault())
                        {
                            table = new Table(item.Items.Count, true);
                        }
                        else
                        {
                            table = new Table(item.Items.Count);
                        }
                        
                    }
                }
                SetBorder(table);
                SetMargin(table);
                SetKeepTogether(table);
                SetKeepWithNext(table);
                SetDestination(table);
                SetInnerAlignment(table);
                SetOuterAlignment(table);
                if (item.Width.HasValue)
                {
                    table.SetWidth(item.Width.Value);
                }
                else
                {
                    if (usePercentWidth || (this.columnWidth == null && !item.WidthStyle.HasValue) || (item.WidthStyle.HasValue && item.WidthStyle.Value == WidthStyle.Full))
                    {
                        table.UseAllAvailableWidth();
                    }
                }
                if (item.Height.HasValue)
                {
                    table.SetHeight(item.Height.Value);
                }
                if (!string.IsNullOrEmpty(item.BackColor))
                {
                    table.SetBackgroundColor(GetColor(item.BackColor));
                }

                if (item.Items != null && sourceData != null && drawItems)
                {
                    if (item.DrawObjectRows.GetValueOrDefault())
                    {
                        IEnumerable listObject = null;
                        if (sourceData is IEnumerable)
                        {
                            listObject = sourceData as IEnumerable;
                        }
                        else if (!string.IsNullOrEmpty(item.ValuePath))
                        {
                            listObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.ValuePath, sourceData) as IEnumerable;
                        }
                        if (listObject != null)
                        {
                            foreach (var valueObj in listObject)
                            {
                                foreach (var subItem in item.Items)
                                {
                                    if (CheckStyle(subItem))
                                    {
                                        if (subItem.Type == ElementType.TableCell)
                                        {
                                            TableCellHandler tableCellHandler = new TableCellHandler(valueObj, subItem, styleName, document);
                                            var cell = tableCellHandler.Handle();

                                            if (cell != null)
                                            {
                                                table.AddCell(cell);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var subItem in item.Items)
                        {
                            if (CheckStyle(subItem))
                            {
                                if (subItem.Type == ElementType.TableCell)
                                {
                                    if (!string.IsNullOrEmpty(subItem.ValuePath) || (subItem.Items == null || subItem.Items.Count <= 0))
                                    {
                                        var valueObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(subItem.ValuePath, sourceData);
                                        if (valueObject != null)
                                        {
                                            if (valueObject.GetType().IsGenericType && valueObject is ICollection)
                                            {
                                                var listValue = valueObject as ICollection;
                                                foreach (var obj in listValue)
                                                {
                                                    var cell = CreateCellForListObject(obj, subItem);

                                                    if (cell != null)
                                                    {
                                                        table.AddCell(cell);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TableCellHandler tableCellHandler = new TableCellHandler(valueObject, subItem, styleName, document, sourceData);
                                                var cell = tableCellHandler.Handle();
                                                if (cell != null)
                                                {
                                                    table.AddCell(cell);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        TableCellHandler tableCellHandler = new TableCellHandler(sourceData, subItem, styleName, document);
                                        var cell = tableCellHandler.Handle();
                                        if (cell != null)
                                        {
                                            table.AddCell(cell);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (item.Position != null)
                {
                    var totalWidth = 0f;
                    if (columnWidth != null && columnWidth.Length > 0)
                    {
                        foreach (var width in columnWidth)
                        {
                            totalWidth += width.GetValue();
                        }
                    }
                    Rectangle pageSize = null;
                    if (document.GetPdfDocument().GetNumberOfPages() > 0)
                    {
                        pageSize = document.GetPdfDocument().GetLastPage().GetPageSize();
                    }
                    else
                    {
                        pageSize = document.GetPdfDocument().GetDefaultPageSize();
                    }
                    if (item.Width.HasValue)
                    {
                        totalWidth = item.Width.Value;
                    }
                    else
                    {
                        
                        var pageWidth = pageSize.GetWidth() - ElementGenerator.DocumentMargins[1] - ElementGenerator.DocumentMargins[3];

                        if (usePercentWidth)
                        {
                            totalWidth = pageWidth * totalWidth / 100;
                        }
                    }

                    var left = 0f;
                    if (!item.Position.Left.HasValue)
                    {
                        if (item.Position.Right.HasValue)
                        {
                            left = pageSize.GetWidth() - item.Position.Right.Value - totalWidth;
                        }
                        else if (item.Position.HorizontalAlignment == HorizontalAlignment.RIGHT)
                        {
                            left = pageSize.GetWidth() - ElementGenerator.DocumentMargins[1] - totalWidth;
                        }
                    }
                    else
                    {
                        left = item.Position.Left.Value;
                    }
                    var bottom = 0f;
                    if (!item.Position.Bottom.HasValue && item.Position.Top.HasValue)
                    {
                        bottom = pageSize.GetHeight() - item.Position.Top.Value;
                    }
                    else
                    {
                        bottom = item.Position.Bottom.Value;
                    }

                    table.SetFixedPosition(left, bottom, totalWidth);


                }
                if (item.BorderAxialShading != null)
                {
                    table.SetNextRenderer(new BorderShadingTableRenderer(table, item.BorderAxialShading));
                }
                if (item.BorderMargin != null)
                {
                    table.SetNextRenderer(new BorderMarginTableRenderer(table, item.BorderMargin));
                }

                if (item.RoundRectangle != null)
                {
                    table.SetNextRenderer(new RoundBorderTableRenderer(table, item.RoundRectangle));
                }

            }

            return table;
        }

        public virtual Cell CreateCellForListObject(object obj, ElementItem subItem)
        {
            TableCellHandler tableCellHandler = new TableCellHandler(obj, subItem, styleName, document, sourceData);
            var cell = tableCellHandler.Handle();
            return cell;
        }
    }
}
