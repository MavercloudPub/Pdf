using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public class TableCellHandler : ElementBaseHandler<Cell>
    {
        private IBlockElement child;

        private List<IBlockElement> childs;

        private object mainSourceData;

        public TableCellHandler(object sourceData, ElementItem item, string styleName, Document document) 
            : base(sourceData, item, styleName, document)
        {
        }

        public TableCellHandler(object sourceData, ElementItem item, string styleName, Document document, float containerWidth)
            : base(sourceData, item, styleName, document, containerWidth)
        {
        }

        public TableCellHandler(object sourceData, ElementItem item, string styleName, Document document, object mainSourceData)
            : base(sourceData, item, styleName, document)
        {
            this.mainSourceData = mainSourceData;
        }

        public TableCellHandler(object sourceData, ElementItem item, string styleName, Document document, object mainSourceData, float containerWidth)
            : base(sourceData, item, styleName, document, containerWidth)
        {
            this.mainSourceData = mainSourceData;
        }

        public void SetChildren(IBlockElement element)
        {
            this.child = element;
        }

        public void SetChildren<T>(List<T> elements) where T : IBlockElement
        {
            this.childs = new List<IBlockElement>();
            elements.ForEach(t => this.childs.Add(t));
        }

        public override Cell Handle()
        {
            Cell cell = null;
            if (CheckStyle())
            {
                if (item.Rowspan.HasValue || item.Colspan.HasValue)
                {
                    cell = new Cell(item.Rowspan.GetValueOrDefault(1), item.Colspan.GetValueOrDefault(1));
                }
                else
                {
                    cell = new Cell();
                }
                if (!string.IsNullOrEmpty(item.BackColor))
                {
                    cell.SetBackgroundColor(GetColor(item.BackColor));
                }
                if (item.Width.HasValue)
                {
                    cell.SetWidth(item.Width.Value);
                }
                if (item.MinWidth.HasValue)
                {
                    cell.SetMinWidth(item.MinWidth.Value);
                }
                if (item.Height.HasValue)
                {
                    cell.SetHeight(item.Height.Value);
                }
                if (item.MinHeight.HasValue)
                {
                    cell.SetMinHeight(item.MinHeight.Value);
                }
                if (sourceData != null)
                {
                    if (sourceData is string || sourceData is ValueType)
                    {
                        var text = sourceData?.ToString();
                        if (text != null)
                        {
                            if (item.Items != null && item.Items.Count == 1)
                            {
                                if (item.Items[0].Type == ElementType.Paragraph)
                                {
                                    var paraHandler = new ParagraphHandler(text, item.Items[0], styleName, document, containerWidth);
                                    var paragraph = paraHandler.Handle();
                                    cell.Add(paragraph);
                                }
                                else if (item.Items[0].Type == ElementType.Image)
                                {
                                    float? fixedWidth = null;
                                    float? fixedHeight = null;
                                    TableDisplayType? tableDisplayType = null;
                                    if (mainSourceData != null)
                                    {
                                        var width = Mavercloud.PDF.Helpers.Object.GetPropertyValue("Width", mainSourceData);
                                        if (width != null && !string.IsNullOrEmpty(width.ToString()))
                                        {
                                            fixedWidth = float.Parse(width.ToString());
                                        }
                                        var height = Mavercloud.PDF.Helpers.Object.GetPropertyValue("Height", mainSourceData);
                                        if (height != null && !string.IsNullOrEmpty(height.ToString()))
                                        {
                                            fixedHeight = float.Parse(height.ToString());
                                        }
                                        var tableDisplayTypeValue = Mavercloud.PDF.Helpers.Object.GetPropertyValue("TableDisplayType", mainSourceData);
                                        if (tableDisplayTypeValue != null)
                                        {
                                            tableDisplayType = Helpers.Enum.Parse<TableDisplayType>(tableDisplayTypeValue);
                                        }
                                    }
                                    var imageHandle = new ImageHandler(text, item.Items[0], styleName, document, fixedWidth, fixedHeight, tableDisplayType);
                                    var image = imageHandle.Handle();
                                    if (image != null)
                                    {
                                        cell.Add(image);
                                    }
                                }
                                else if (item.Items[0].Type == ElementType.Table)
                                {
                                    var tableHandler = new TableHandler(text, item.Items[0], styleName, document);
                                    cell.Add(tableHandler.Handle());
                                }
                            }
                            else
                            {
                                var paraHandler = new ParagraphHandler(sourceData, item, styleName, document, containerWidth);
                                var paragraph = paraHandler.Handle();
                                paragraph.SetBorder(Border.NO_BORDER);
                                cell.Add(paragraph);
                            }
                        }
                    }
                    else if (item.ListValueStyleType.HasValue && item.ListValueStyleType == ListValueStyleType.MultipleElements)
                    {
                        if (sourceData.GetType().IsGenericType && sourceData is IEnumerable)
                        {
                            var listObject = sourceData as IEnumerable;
                            foreach (var valueObj in listObject)
                            {
                                CreateChildren(cell, valueObj);
                            }
                        }
                    }
                    else if (item.Items != null && item.Items.Count > 0)
                    {
                        CreateChildren(cell);
                    }
                }
                else if (this.child != null)
                {
                    cell.Add(child);
                }
                else if (this.childs != null)
                {
                    this.childs.ForEach(t => cell.Add(t));
                }

                this.SetStyle(cell);
                SetKeepTogether(cell);
                if (item.KeepTogetherWhenNoneRowSpan.GetValueOrDefault())
                {
                    if (item.Rowspan.GetValueOrDefault(1) == 1)
                    {
                        cell.SetKeepTogether(true);
                    }
                }
                if (item.BackAxialShading != null)
                {
                    // Use a custom renderer in which drawing is overridden
                    cell.SetNextRenderer(new BackgroundShadingCellRenderer(cell, item.BackAxialShading));
                }
                if (item.BorderMargin != null)
                {
                    cell.SetNextRenderer(new BorderMarginCellRenderer(cell, item.BorderMargin));
                }
                if (item.RotationRatio.HasValue)
                {
                    cell.SetRotationAngle(Math.PI / item.RotationRatio.Value);
                }
                if (item.CustomHeightBorder != null)
                {
                    cell.SetNextRenderer(new CustomizeHeightBorderCellRenderer(cell, item.CustomHeightBorder));
                }

                if (item.RoundRectangle != null)
                {
                    cell.SetNextRenderer(new RoundTableCellRenderer(cell, item.RoundRectangle));
                }

            }
            return cell;
        }
    }
}
