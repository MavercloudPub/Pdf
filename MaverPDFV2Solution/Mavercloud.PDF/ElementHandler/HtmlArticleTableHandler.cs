using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Mavercloud.PDF.ElementHandler
{
    public class HtmlArticleTableHandler : HtmlArticleTableBaseHandler<Table>
    {
        

        public HtmlArticleTableHandler(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            string styleName, 
            Document document, 
            TableType? tableType, 
            TableDisplayType? tableDisplayType) : this(sourceData,
                tableXmlNode, tableItem, null, styleName, document, tableType, tableDisplayType)
        {
        }

        public HtmlArticleTableHandler(object sourceData,
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
            tableHeaderItem = item.Items.First(t => t.Name == "TableHeader");
            tableCellItem = item.Items.First(t => t.Name == "TableContent");
            footNotesItem = item.Items.First(t => t.Name == "TableFootnotes");
        }

        public override Table Handle()
        {
            Table table = null;

            CreateColumnMatrix();

            var tableMaxWidth = GetTableMaxWidth();

            
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

            //暂时不支持表格翻转
            rotation = false;

            if (!rotation)
            {
                item.LargeTable = false;

                var pageWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[1] - ElementGenerator.DocumentMargins[3];

                InitializeColumnWidthArray(pageWidth, false);

                var colCount = GetColumnCount();

                if (tableType.HasValue && tableType == TableType.Large)
                {
                    item.LargeTable = true;
                }

                TableHandler tableHandler = null;

                //ignore the calculated column width, using autowidth by iText
                fixedColumnWidth = false;

                if (fixedColumnWidth)
                {
                    tableHandler = new TableHandler(sourceData, item, styleName, UnitValue.CreatePointArray(columnWidthArray), false, document);
                }
                else
                {
                    tableHandler = new TableHandler(sourceData, item, styleName, colCount, false, document);
                }

                table = tableHandler.Handle();
                //table.UseAllAvailableWidth();

                var fixedTableWidth = false;
                var tableWidth = Helpers.Object.GetFollowingPropertyValue("Width", sourceData);
                if (tableWidth != null)
                {
                    fixedTableWidth = true;
                    table.SetWidth(Helpers.Convert.ToFloat(tableWidth));
                    table.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                }

                captionItem.Colspan = colCount;
                captionItem.Rowspan = 1;

                var captionCellHandler = new TableCellHandler(sourceData, captionItem, styleName, document, Helpers.Convert.ToFloatOrNull(tableWidth));
                var captionCell = captionCellHandler.Handle();
                table.AddCell(captionCell);
                //if (fixedTableWidth)
                //{
                //    foreach (var captionParagraph in captionCell.GetChildren())
                //    {
                //        if (captionParagraph is Paragraph)
                //        {
                //            ((Paragraph)captionParagraph).SetTextAlignment(TextAlignment.CENTER);
                //        }
                //    }
                //}

                var captionMarginItem = this.item.Items.FirstOrDefault(t => t.Name == "TableCaptionMargin");
                if (captionMarginItem != null)
                {
                    captionMarginItem.Colspan = colCount;
                    captionMarginItem.Rowspan = 1;
                    var captionMarginCellHandler = new TableCellHandler(string.Empty, captionMarginItem, styleName, document);
                    var captionMarginCell = captionMarginCellHandler.Handle();
                    table.AddCell(captionMarginCell);
                }

                foreach (var kvp in rowCellsDic)
                {
                    foreach (var prepareCell in kvp.Value)
                    {
                        var currentCellItem = tableCellItem;
                        if (prepareCell.IsHeader)
                        {
                            currentCellItem = tableHeaderItem;
                        }
                        currentCellItem.Rowspan = prepareCell.Rowspan;
                        currentCellItem.Colspan = prepareCell.Colspan;
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
                        bodyCell = bodyCellHandler.Handle();
                        if (prepareCell.Width > 0)
                        {
                            bodyCell.SetWidth(prepareCell.Width);
                        }

                        if (!string.IsNullOrEmpty(prepareCell.BackColor))
                        {
                            bodyCell.SetBackgroundColor(ElementGenerator.GetColor(prepareCell.BackColor));
                        }

                        table.AddCell(bodyCell);
                    }
                }

                var bodyMarginItem = this.item.Items.FirstOrDefault(t => t.Name == "TableContentMargin");
                if (bodyMarginItem != null)
                {
                    bodyMarginItem.Colspan = colCount;
                    bodyMarginItem.Rowspan = 1;
                    var bodyMarginCellHandler = new TableCellHandler(string.Empty, bodyMarginItem, styleName, document);
                    var bodyMarginCell = bodyMarginCellHandler.Handle();
                    table.AddCell(bodyMarginCell);
                }
                var footnotesListObj = Helpers.Object.GetFollowingPropertyValue(footNotesItem.ValuePath, sourceData);

                if (footnotesListObj != null && (footnotesListObj as IList).Count > 0)
                {
                    footNotesItem.Colspan = colCount;
                    footNotesItem.Rowspan = 1;

                    var footNotesCellHandler = new TableCellHandler(Helpers.Object.GetFollowingPropertyValue(footNotesItem.ValuePath, sourceData), footNotesItem, styleName, document);
                    var footNotesCell = footNotesCellHandler.Handle();
                    table.AddCell(footNotesCell);
                }

                //针对LargeTable Margin失效，通过增加空单元格实现MarginBottom
                if (item.LargeTable.GetValueOrDefault())
                {
                    var emptyCell = new Cell(1, colCount);
                    emptyCell.SetHeight(item.MarginBottom.GetValueOrDefault() - 5).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.WHITE);
                    table.AddCell(emptyCell);
                }
            }
            else
            {
                captionItem = item.Items.First(t => t.Name == "RotationTableCaption");
                tableHeaderItem = item.Items.First(t => t.Name == "RotationTableHeader");
                tableCellItem = item.Items.First(t => t.Name == "RotationTableContent");
                footNotesItem = item.Items.First(t => t.Name == "RotationTableFootnotes");

                //表格翻转后的行数
                var rowCount = GetColumnCount();

                //表格翻转后的列数
                int colCount = 0;

                //初始化单元格高度（翻转)
                var pageHeight = document.GetPdfDocument().GetDefaultPageSize().GetHeight() - ElementGenerator.DocumentMargins[0] - ElementGenerator.DocumentMargins[2];
                var tableRotationWidth = tableMaxWidth;
                if (tableRotationWidth > pageHeight * 0.8f)
                {
                    tableRotationWidth = pageHeight * 0.8f - float.Parse(rowCount.ToString()) * 6f;
                }

                InitializeColumnWidthArray(tableRotationWidth, true);

                var tableHeaderCellParaItem = tableHeaderItem.Items.First(t => t.Type == ElementType.Paragraph);
                var tableBodyCellParaItem = tableCellItem.Items.First(t => t.Type == ElementType.Paragraph);


                colCount += rowCellsDic.Count;
                colCount++;//增加Title列

                //增加Margin列数
                var captionMarginItem = this.item.Items.FirstOrDefault(t => t.Name == "RotationTableCaptionMargin");
                var bodyMarginItem =  this.item.Items.FirstOrDefault(t => t.Name == "RotationTableContentMargin");

                if (captionMarginItem != null)
                {
                    colCount++;
                }
                if (bodyMarginItem != null)
                {
                    colCount++;
                }

                //增加Footnotes列数
                var footNotesListObj = Helpers.Object.GetFollowingPropertyValue(footNotesItem.ValuePath, sourceData);
                if (footNotesListObj != null)
                {
                    colCount += (footNotesListObj as IList).Count;
                }

                item.WidthStyle = WidthStyle.Full;
                var tableHandler = new TableHandler(sourceData, item, styleName, colCount, false, document);
                table = tableHandler.Handle();

                //var tableHeight = 0f;
                //var tableMaxWidth = GetTableMaxWidth();
                //float bodyParaWidthRadio = 1f;
                //if (tableMaxWidth > pageHeight * 0.9f)
                //{
                //    bodyParaWidthRadio = pageHeight * 0.9f / tableMaxWidth;
                //}
                //else
                //{
                //    tableHeight = tableMaxWidth;
                //}


                captionItem.Rowspan = rowCount;
                captionItem.Colspan = 1;

                var captionCellHandler = new TableCellHandler(sourceData, captionItem, styleName, document);
                var captionCell = captionCellHandler.Handle();

                var captionCellParagraph = captionCell.GetChildren()[0] as Paragraph;

                table.AddCell(captionCell);


                if (captionMarginItem != null)
                {
                    captionMarginItem.Rowspan = rowCount;
                    captionMarginItem.Colspan = 1;
                    var captionMarginCellHandler = new TableCellHandler(string.Empty, captionMarginItem, styleName, document);
                    var captionMarginCell = captionMarginCellHandler.Handle();
                    table.AddCell(captionMarginCell);
                }

                List<Cell> footNotesCells = new List<Cell>();

                for (int i = rowCount - 1; i >= 0; i--)
                {
                    for (int j = 0; j < rowCellsDic.Count; j++)
                    {
                        var prepareCell = GetPreparingCell(j, i);
                        if (prepareCell != null)
                        {
                            var currentCellItem = tableCellItem;
                            if (prepareCell.IsHeader)
                            {
                                currentCellItem = tableHeaderItem;
                            }
                            currentCellItem.Rowspan = prepareCell.Colspan;
                            currentCellItem.Colspan = prepareCell.Rowspan;
                            if (prepareCell.BorderBottom)
                            {
                                currentCellItem.BorderBottom = null;
                                currentCellItem.BorderRight = headerCellBorder;
                            }
                            else
                            {
                                currentCellItem.BorderBottom = null;
                                currentCellItem.BorderRight = null;
                            }

                            var bodyCellHandler = new TableCellHandler(null, currentCellItem, styleName, document);
                            var bodyCell = bodyCellHandler.Handle();

                            prepareCell.paragraph.SetRotationAngle(Math.PI / 2);

                            //prepareCell.paragraph.SetWidth(100f);

                            //prepareCell.paragraph.SetWidth(prepareCell.Width);
                            if (fixedColumnWidth)
                            {
                                if (currentCellItem.Rowspan > 1)
                                {
                                    var multiColumnWidth = 0f;
                                    for (int ir = 0; ir < currentCellItem.Rowspan; ir++)
                                    {
                                        multiColumnWidth += columnWidthArray[i + ir];
                                    }
                                    prepareCell.paragraph.SetWidth(multiColumnWidth);
                                }
                                else
                                {
                                    prepareCell.paragraph.SetWidth(columnWidthArray[i]);
                                }
                            }
                            else
                            {
                                prepareCell.paragraph.SetWidth(prepareCell.Width);
                            }


                            if (!string.IsNullOrEmpty(prepareCell.BackColor))
                            {
                                bodyCell.SetBackgroundColor(ElementGenerator.GetColor(prepareCell.BackColor));
                            }

                            if (prepareCell.IsHeader)
                            {
                                var paragrahHandler = new ParagraphHandler(prepareCell.paragraph, tableHeaderCellParaItem, styleName, document);
                                paragrahHandler.SetStyle();
                            }
                            else
                            {
                                var paragrahHandler = new ParagraphHandler(prepareCell.paragraph, tableBodyCellParaItem, styleName, document);
                                paragrahHandler.SetStyle();
                            }                            
                            //var paraHeight = prepareCell.paragraph.GetHeightOnRendering(prepareCell.paragraph.GetWidth().GetValue(), pageHeight, document);
                            //bodyCell.SetWidth(paraHeight + 2f);

                            bodyCell.Add(prepareCell.paragraph);

                            table.AddCell(bodyCell);

                            //Console.WriteLine(string.Format("{0} {1} {2} {3} {4}", i, j, prepareCell.Colspan, prepareCell.Rowspan, prepareCell.InnerXml));
                        }
                    }
                    if (i == rowCount - 1)
                    {
                        if (bodyMarginItem != null)
                        {
                            bodyMarginItem.Rowspan = rowCount;
                            bodyMarginItem.Colspan = 1;
                            var bodyMarginCellHandler = new TableCellHandler(string.Empty, bodyMarginItem, styleName, document);
                            var bodyMarginCell = bodyMarginCellHandler.Handle();
                            table.AddCell(bodyMarginCell);
                        }
                        if (footNotesListObj != null && (footNotesListObj as IList).Count > 0)
                        {
                            footNotesItem.Rowspan = rowCount;
                            footNotesItem.Colspan = 1;

                            foreach (var footObj in (footNotesListObj as IEnumerable))
                            {
                                var footNotesCellHandler = new TableCellHandler(footObj, footNotesItem, styleName, document);
                                var footNotesCell = footNotesCellHandler.Handle();

                                footNotesCells.Add(footNotesCell);

                                table.AddCell(footNotesCell);
                            }
                        }
                    }
                }

                var tableHeight = table.GetHeightOnRendering(document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[3] - ElementGenerator.DocumentMargins[1], 1000000f, document);
                captionCellParagraph.SetWidth(tableHeight - 5f);

                if (footNotesCells != null && footNotesCells.Count > 0)
                {
                    foreach (var footNotesCell in footNotesCells)
                    {
                        foreach (var child in footNotesCell.GetChildren())
                        {
                            if (child is Paragraph)
                            {
                                (child as Paragraph).SetWidth(tableHeight - 2f);
                            }
                        }
                    }
                }

            }

            //Console.ReadLine();
            return table;
        }

        protected override List CreateTableCellList(string innerXml)
        {
            throw new NotImplementedException();
        }
    }

   
}
