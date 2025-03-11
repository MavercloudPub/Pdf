using Force.DeepCloner;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.EventHandler;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Mavercloud.PDF.ElementHandler
{
    public class HtmlArticleTableDivHandler : HtmlArticleTableBaseHandler<Div>
    {
        private Table table;

        private bool textAlignCenterWhenHeaderMultipleColspan = false;

        private bool cellBottomBorderWhenHeaderMultipleColspan = false;

        private bool maxColumnCellWidthAsCellMinWidth = false;

        private bool enabledMaxColumnCellWidthAsCellMinWidth = true;

        private float? columnWidth;


        public HtmlArticleTableDivHandler(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            string styleName,
            Document document,
            TableType? tableType,
            TableDisplayType? tableDisplayType) : this(sourceData,
                tableXmlNode, tableItem, null, styleName, document, tableType, tableDisplayType)
        {
        }

        public HtmlArticleTableDivHandler(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            BorderStyle headerCellBorder,
            string styleName,
            Document document,
            TableType? tableType,
            TableDisplayType? tableDisplayType)
            : base(sourceData, tableXmlNode, tableItem, headerCellBorder, styleName, document, tableType, tableDisplayType)
        {

            borderDefinitedInSytleItem = false;

            captionItem = item.Items.First(t => t.Name == "TableCaption");
            graphicItem = item.Items.FirstOrDefault(t => t.Name == "TableGraphics");
            tableTableItem = item.Items.First(t => t.Name == "SectionTableTable");
            tableHeaderItem = tableTableItem.Items.First(t => t.Name == "TableHeader");
            tableCellItem = tableTableItem.Items.First(t => t.Name == "TableContent");
            footNotesItem = item.Items.First(t => t.Name == "TableFootnotes");
        }

        public void SetTextAlignCenterWhenHeaderMultipleColspan(bool value)
        {
            this.textAlignCenterWhenHeaderMultipleColspan = value;
        }

        public void SetCellBottomBorderWhenHeaderMultipleColspan(bool value)
        {
            this.cellBottomBorderWhenHeaderMultipleColspan = true;
        }

        public void SetTableColumnMinWidths(Dictionary<int, float> widths)
        {
            this.tableColumnMinWidths = widths;
        }

        public void SetEnabledMaxColumnCellWidthAsCellMinWidth(bool value)
        {
            enabledMaxColumnCellWidthAsCellMinWidth = value;
        }

        public void SetColumnWidthWhenTableFollowingColumnParagraph(float value)
        {
            this.columnWidth = value;
        }
        public override Div Handle()
        {
            Div div = null;

            CreateColumnMatrix();
            var tableMaxWidth = GetTableMaxWidth();

            var pageWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - document.GetRightMargin() - document.GetLeftMargin();
            if (columnWidth.HasValue)
            {
                pageWidth = columnWidth.Value;
            }
            if (columnMaxWidthsArray.Sum() < pageWidth 
                && (tableColumnMinWidths == null || tableColumnMinWidths.Count <= 0)
                && enabledMaxColumnCellWidthAsCellMinWidth)
            {
                maxColumnCellWidthAsCellMinWidth = true;
                var exWidth = pageWidth - columnMaxWidthsArray.Sum();
                var average = exWidth / columnMaxWidthsArray.Count();
                for (var ci = 0; ci < columnMaxWidthsArray.Count; ci++)
                {
                    columnMaxWidthsArray[ci] = columnMaxWidthsArray[ci] + average;
                }
            }

            var divHandler = new DivHandler(null, item, styleName, document);
            div = divHandler.Handle();

            var captionParagraph = CreateCaptionParagraph();

            div.Add(captionParagraph);

            if (graphicItem != null)
            {
                var graphicListObj = Helpers.Object.GetFollowingPropertyValue(graphicItem.ValuePath, sourceData);
                if (graphicListObj != null && (graphicListObj as IList).Count > 0)
                {
                    foreach (var graphicObj in graphicListObj as IList)
                    {
                        var graphicParagraph = CreateGraphicParagraph(graphicObj);
                        div.Add(graphicParagraph);
                    }
                }
            }

            rotation = IsTableRotation(tableMaxWidth);

            //不支持表格翻转的情况下，对于大型表格，设置页面翻转
            if (rotation && !this.tableDisplayType.HasValue)
            {
                this.tableDisplayType = TableDisplayType.PageRotation;
            }
            else
            {
                if (!this.tableDisplayType.HasValue)
                {
                    this.tableDisplayType = TableDisplayType.Normal;
                }
            }

            tableTableItem.LargeTable = false;

            var colCount = GetColumnCount();

            if (tableType.HasValue && tableType == TableType.Large)
            {
                tableTableItem.LargeTable = true;
            }


            TableHandler tableHandler = null;

            //ignore the calculated column width, using autowidth by iText
            fixedColumnWidth = false;

            if (fixedColumnWidth)
            {
                tableHandler = new TableHandler(sourceData, tableTableItem, styleName, UnitValue.CreatePointArray(columnWidthArray), false, document);
            }
            else
            {
                tableHandler = new TableHandler(sourceData, tableTableItem, styleName, colCount, false, document);
            }

            table = tableHandler.Handle();


            var fixedTableWidth = false;
            var tableWidth = Helpers.Object.GetFollowingPropertyValue("Width", sourceData);
            if (tableWidth != null)
            {
                fixedTableWidth = true;
                table.SetWidth(Helpers.Convert.ToFloat(tableWidth));
                table.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                
            }
            else
            {
                captionParagraph.SetTextAlignment(TextAlignment.JUSTIFIED);
            }

            if (fixedTableWidth)
            {
                if (item.CaptionFootnotesSameWidthWithTable.GetValueOrDefault())
                {
                    captionParagraph.SetWidth(Helpers.Convert.ToFloat(tableWidth));
                    captionParagraph.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                }
            }

            var headerCellItem = item.Items.FirstOrDefault(t => t.Name == "HeaderCell");
            if (headerCellItem != null)
            {
                headerCellItem.Colspan = colCount;
                headerCellItem.Rowspan = 1;
                var headerCellHandler = new TableCellHandler("", headerCellItem, styleName, document);
                var headerCell = headerCellHandler.Handle();
                table.AddHeaderCell(headerCell).SetSkipFirstHeader(true);
            }
            var headerMarginItem = this.tableTableItem.Items.FirstOrDefault(t => t.Name == "TableHeaderMargin");
            if (headerMarginItem != null)
            {
                headerMarginItem.Colspan = colCount;
                headerMarginItem.Rowspan = 1;
                var headerMarginCellHandler = new TableCellHandler(string.Empty, headerMarginItem, styleName, document);
                var headerMarginCell = headerMarginCellHandler.Handle();
                table.AddCell(headerMarginCell);
            }

            var headerTextAlignment = tableHeaderItem.TextAlignment;
            var headerCellBorderBottom = tableHeaderItem.BorderBottom;
            if (headerCellBorder == null)
            {
                headerCellBorder = headerCellBorderBottom;
            }

            int rowsSpan = 0;
            var lastRowSpan = 0;
            var paddingLeftHeaderColumns = new List<int>();
            foreach (var kvp in rowCellsDic)
            {
                var col = 0;
                foreach (var prepareCell in kvp.Value)
                {
                    var currentCellItem = tableCellItem;
                    if (prepareCell.IsHeader)
                    {
                        currentCellItem = tableHeaderItem;
                        currentCellItem.TextAlignment = headerTextAlignment;
                        if (prepareCell.Colspan > 1 && textAlignCenterWhenHeaderMultipleColspan)
                        {
                            currentCellItem.TextAlignment = TextAlignment.CENTER;
                        }
                        if (prepareCell.Colspan > 1 && cellBottomBorderWhenHeaderMultipleColspan)
                        {
                            prepareCell.BorderBottom = true;
                        }
                    }
                    currentCellItem.Rowspan = prepareCell.Rowspan;
                    currentCellItem.Colspan = prepareCell.Colspan;

                    if (prepareCell.Colspan == 1 
                        && currentCellItem.CellColumnWidthEnabled.GetValueOrDefault())
                    {
                        if (prepareCell.MinWidth == 0)
                        {
                            if (tableColumnMinWidths != null && tableColumnMinWidths.ContainsKey(prepareCell.ColumnIndex + 1))
                            {
                                prepareCell.MinWidth = tableColumnMinWidths[prepareCell.ColumnIndex + 1];
                            }
                            else if (maxColumnCellWidthAsCellMinWidth)
                            {
                                prepareCell.MinWidth = columnMaxWidthsArray[prepareCell.ColumnIndex];
                            }
                        }
                    }
                    if (!borderDefinitedInSytleItem)
                    {
                        if (prepareCell.BorderBottom)
                        {
                            currentCellItem.BorderBottom = headerCellBorder;
                        }
                        else
                        {
                            currentCellItem.BorderBottom = null;
                        }
                    }
                    
                    if (prepareCell.TextAlignment.HasValue)
                    {
                        currentCellItem.TextAlignment = prepareCell.TextAlignment;
                    }
                    

                    Cell bodyCell = null;

                    var bodyCellHandler = new TableCellHandler(null, currentCellItem, styleName, document);
                    if (prepareCell.paragraph != null)
                    {
                        bodyCellHandler.SetChildren(prepareCell.paragraph);
                    }
                    else if (prepareCell.Elements != null && prepareCell.Elements.Count > 0)
                    {
                        bodyCellHandler.SetChildren(prepareCell.Elements);
                    }
                    else if (prepareCell.Paragraphs != null && prepareCell.Paragraphs.Count > 0)
                    {
                        bodyCellHandler.SetChildren(prepareCell.Paragraphs);
                    }
                    else if (prepareCell.List != null)
                    {
                        bodyCellHandler.SetChildren(prepareCell.List);
                    }
                    bodyCell = bodyCellHandler.Handle();

                    if (currentCellItem.TextCellWidthEnabled.GetValueOrDefault())
                    {
                        if (!string.IsNullOrEmpty(prepareCell.InnerXml))
                        {
                            var paragraphInnerText = GetInnerText(prepareCell.InnerXml);
                            if (!paragraphInnerText.Contains(" "))
                            {
                                var minWidth = 0f;
                                if (prepareCell.Colspan >= 1)
                                {
                                    for (int ci = prepareCell.ColumnIndex; ci < prepareCell.ColumnIndex + prepareCell.Colspan; ci++)
                                    {
                                        minWidth += columnRowWidthDic[ci][prepareCell.RowIndex];
                                    }
                                }
                                else
                                {
                                    minWidth = columnRowWidthDic[prepareCell.ColumnIndex][prepareCell.RowIndex];
                                }
                                if (prepareCell.Width == 0 && prepareCell.MinWidth < minWidth)
                                {
                                    prepareCell.MinWidth = minWidth;
                                }
                            }
                        }
                    }

                    if (prepareCell.Width > 0)
                    {
                        bodyCell.SetWidth(prepareCell.Width);
                    }
                    if (prepareCell.MinWidth > 0)
                    {
                        bodyCell.SetMinWidth(prepareCell.MinWidth);
                    }

                    if (!string.IsNullOrEmpty(prepareCell.BackColor))
                    {
                        bodyCell.SetBackgroundColor(ElementGenerator.GetColor(prepareCell.BackColor));
                    }

                    if (prepareCell.IsHeader && item.UseHeaderCell.GetValueOrDefault())
                    {
                        if (tableHeaderItem.PaddingLeftExceptFirst.HasValue && (col > 0 || (col == 0 && rowsSpan > 0)))
                        {
                            bodyCell.SetPaddingLeft(tableHeaderItem.PaddingLeftExceptFirst.Value);
                        }
                        else if (paddingLeftHeaderColumns.Contains(prepareCell.ColumnIndex) || 
                            (tableHeaderItem.PaddingRight.HasValue 
                            && tableHeaderItem.PaddingLeft.GetValueOrDefault() == 0 
                            && col > 0 && prepareCell.Rowspan > lastRowSpan))
                        {
                            bodyCell.SetPaddingLeft(tableHeaderItem.PaddingRight.Value);
                            if (!paddingLeftHeaderColumns.Contains(prepareCell.ColumnIndex))
                            {
                                paddingLeftHeaderColumns.Add(prepareCell.ColumnIndex);
                            }
                        }
                        table.AddHeaderCell(bodyCell);
                    }
                    else
                    {
                        if (tableCellItem.PaddingLeftExceptFirst.HasValue && (col > 0 || (col == 0 && (rowsSpan > 0 || prepareCell.Rowspan > 0))))
                        {
                            bodyCell.SetPaddingLeft(tableCellItem.PaddingLeftExceptFirst.Value);
                        }
                        else if (paddingLeftHeaderColumns.Contains(prepareCell.ColumnIndex))
                        {
                            bodyCell.SetPaddingLeft(tableCellItem.PaddingRight.Value);
                        }
                        table.AddCell(bodyCell);
                    }
                    if (col == 0 && prepareCell.Rowspan > 1)
                    {
                        rowsSpan = prepareCell.Rowspan;
                    }
                    if (col == 0)
                    {
                        rowsSpan--;
                    }
                    lastRowSpan = prepareCell.Rowspan;
                    col += prepareCell.Colspan;
                }
            }

            var bodyMarginItem = this.tableTableItem.Items.FirstOrDefault(t => t.Name == "TableContentMargin");
            if (bodyMarginItem != null)
            {
                bodyMarginItem.Colspan = colCount;
                bodyMarginItem.Rowspan = 1;
                var bodyMarginCellHandler = new TableCellHandler(string.Empty, bodyMarginItem, styleName, document);
                var bodyMarginCell = bodyMarginCellHandler.Handle();
                table.AddCell(bodyMarginCell);
            }

            var footerCellItem = item.Items.FirstOrDefault(t => t.Name == "FooterCell");
            if (footerCellItem != null)
            {
                footerCellItem.Colspan = colCount;
                footerCellItem.Rowspan = 1;
                var footerCellHandler = new TableCellHandler("", footerCellItem, styleName, document);
                var footerCell = footerCellHandler.Handle();
                table.AddFooterCell(footerCell).SetSkipLastFooter(true);
            }
            
            div.Add(table);

            

            SetDestination(div);

            return div;
        }

        protected override List CreateTableCellList(string innerXml)
        {
            throw new NotImplementedException();
        }

        protected virtual Paragraph CreateCaptionParagraph()
        {
            var captionParagraphHandler = new ParagraphHandler(sourceData, captionItem, styleName, document);
            var captionParagraph = captionParagraphHandler.Handle();
            return captionParagraph;
        }

        protected virtual Paragraph CreateGraphicParagraph(object graphicObj)
        {
            var graphicParagraphHandler = new ParagraphHandler(graphicObj, graphicItem.Items[0], styleName, document);
            var graphicParagraph = graphicParagraphHandler.Handle();
            return graphicParagraph;
        }

        

        public Table ArticleTable
        {
            get
            {
                return table;
            }
        }
    }
}
