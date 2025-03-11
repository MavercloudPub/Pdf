using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Mavercloud.PDF.ElementHandler
{
    public class HtmlArticleTableHandlerV2
    {
        private Document document;
        private object sourceData;
        private string styleName;
        private float pageWidth;
        private float pageHeight;

        private ElementItem tableItem;
        private ElementItem captionItem;
        private ElementItem tableHeaderItem;
        private ElementItem tableCellItem;
        private ElementItem footNotesItem;
        private BorderStyle headerCellBorder;
        private bool borderDefinitedInSytleItem;

        private Dictionary<int, float[]> columnRowWidthDic = new Dictionary<int, float[]>();

        private float[] columnWidthArray;

        private bool fixedColumnWidth = false;

        private Dictionary<int, List<PreparingCell>> rowCellsDic = new Dictionary<int, List<PreparingCell>>();

        private XmlNode tableXmlNode;

        private bool? rotation;

        private bool? tableWidthConsistentWithTitle;

        private int headerRowCount;
        public HtmlArticleTableHandlerV2(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            string styleName, Document document, bool? rotation, bool? tableWidthConsistentWithTitle = false) : this(sourceData,
                tableXmlNode, tableItem, null, styleName, document, rotation, tableWidthConsistentWithTitle)
        {
            borderDefinitedInSytleItem = true;
        }
        public HtmlArticleTableHandlerV2(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            BorderStyle headerCellBorder,
            string styleName, Document document, bool? rotation, bool? tableWidthConsistentWithTitle = false)
        {
            this.sourceData = sourceData;
            this.document = document;
            this.styleName = styleName;
            this.tableXmlNode = tableXmlNode;
            this.headerCellBorder = headerCellBorder;
            this.rotation = rotation;
            this.tableWidthConsistentWithTitle = tableWidthConsistentWithTitle;

            captionItem = tableItem.Items.First(t => t.Name == "TableCaption");
            tableHeaderItem = tableItem.Items.First(t => t.Name == "TableHeader");
            tableCellItem = tableItem.Items.First(t => t.Name == "TableContent");
            footNotesItem = tableItem.Items.First(t => t.Name == "TableFootnotes");

            pageWidth = document.GetPageEffectiveArea(document.GetPdfDocument().GetDefaultPageSize()).GetWidth();
            pageHeight = document.GetPageEffectiveArea(document.GetPdfDocument().GetDefaultPageSize()).GetHeight();
        }

        public List<Table> Handle()
        {
            List<Table> tables = new List<Table>();
            CreateColumnMatrix();

            var tableMaxWidth = GetTableMaxWidth();

            if (!rotation.HasValue)
            {
                rotation = IsTableRotation(tableMaxWidth);
            }

            var tableRealWidth = 0f;
            if (!rotation.Value)
            {
                tableRealWidth = pageWidth;
            }
            else
            {
                tableRealWidth = pageHeight;
            }


            InitializeColumnWidthArray(tableRealWidth, rotation.Value);

            if (!rotation.Value)
            {
                captionItem = tableItem.Items.First(t => t.Name == "TableCaption");
                tableHeaderItem = tableItem.Items.First(t => t.Name == "TableHeader");
                tableCellItem = tableItem.Items.First(t => t.Name == "TableContent");
                footNotesItem = tableItem.Items.First(t => t.Name == "TableFootnotes");

                var colCount = GetColumnCount();

                TableHandler tableHandler = null;
                if (fixedColumnWidth)
                {
                    tableHandler = new TableHandler(sourceData, tableItem, styleName, UnitValue.CreatePointArray(columnWidthArray), false, document);
                }
                else
                {
                    tableHandler = new TableHandler(sourceData, tableItem, styleName, colCount, false, document);
                }

                var table = tableHandler.Handle();

                captionItem.Colspan = colCount;
                captionItem.Rowspan = 1;

                var captionCellHandler = new TableCellHandler(sourceData, captionItem, styleName, document);
                var captionCell = captionCellHandler.Handle();
                table.AddCell(captionCell);

                var captionMarginItem = this.tableItem.Items.FirstOrDefault(t => t.Name == "TableCaptionMargin");
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
                        currentCellItem.HorizontalAlignment = prepareCell.HorizontalAlignment;

                        var bodyCellHandler = new TableCellHandler(null, currentCellItem, styleName, document);
                        var bodyCell = bodyCellHandler.Handle();
                        bodyCell.Add(prepareCell.paragraph);
                        table.AddCell(bodyCell);
                    }
                }

                var bodyMarginItem = this.tableItem.Items.FirstOrDefault(t => t.Name == "TableContentMargin");
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
            }
            else
            {
                captionItem = tableItem.Items.First(t => t.Name == "RotationTableCaption");
                tableHeaderItem = tableItem.Items.First(t => t.Name == "RotationTableHeader");
                tableCellItem = tableItem.Items.First(t => t.Name == "RotationTableContent");
                footNotesItem = tableItem.Items.First(t => t.Name == "RotationTableFootnotes");

                //表格翻转后的行数
                var rowCount = GetColumnCount();

                //表格翻转后的列数
                int colCount = 0;

                //初始化单元格高度（翻转)
                var pageHeight = document.GetPdfDocument().GetDefaultPageSize().GetHeight() - ElementGenerator.DocumentMargins[0] - ElementGenerator.DocumentMargins[2];
                var tableRotationWidth = tableMaxWidth;
                if (tableRotationWidth > pageHeight)
                {
                    tableRotationWidth = pageHeight;
                }

                InitializeColumnWidthArray(tableRotationWidth, true);

                var tableHeaderCellParaItem = tableHeaderItem.Items.First(t => t.Type == ElementType.Paragraph);
                var tableBodyCellParaItem = tableCellItem.Items.First(t => t.Type == ElementType.Paragraph);


                colCount += rowCellsDic.Count;
                colCount++;//增加Title列

                //增加Margin列数
                var captionMarginItem = this.tableItem.Items.FirstOrDefault(t => t.Name == "RotationTableCaptionMargin");
                var bodyMarginItem = this.tableItem.Items.FirstOrDefault(t => t.Name == "RotationTableContentMargin");

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

                tableItem.WidthStyle = WidthStyle.Full;
                var tableHandler = new TableHandler(sourceData, tableItem, styleName, colCount, false, document);
                var table = tableHandler.Handle();

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
                            currentCellItem.HorizontalAlignment = prepareCell.HorizontalAlignment;
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
            return tables;
        }

        public bool GetRotation()
        {
            return rotation.GetValueOrDefault();
        }

        private int GetColumnCount()
        {
            int cols = 0;

            //只检测第一行
            foreach (XmlNode tempNode in tableXmlNode.SelectSingleNode("//tr").ChildNodes)
            {
                if (tempNode.NodeType == XmlNodeType.Element && tempNode.Name == "td")
                {
                    if (tempNode.Attributes["colspan"] != null)
                    {
                        cols += int.Parse(tempNode.Attributes["colspan"].Value);
                    }
                    else
                    {
                        cols++;
                    }
                }
            }
            return cols;
        }

        private void CreateColumnMatrix()
        {
            var colCount = GetColumnCount();
            var rowCount = tableXmlNode.SelectNodes("//tr").Count;

            for (int i = 0; i < colCount; i++)
            {
                var widthArray = new float[rowCount];
                for (int j = 0; j < rowCount; j++)
                {
                    widthArray[j] = -1f;
                }
                columnRowWidthDic.Add(i, widthArray);
            }
            for (int i = 0; i < rowCount; i++)
            {
                rowCellsDic.Add(i, new List<PreparingCell>());
            }

            var tableHeaderCellParaItem = tableHeaderItem.Items.First(t => t.Type == ElementType.Paragraph);
            var tableBodyCellParaItem = tableCellItem.Items.First(t => t.Type == ElementType.Paragraph);

            XmlNodeList headerRowList = tableXmlNode.SelectNodes("./thead/tr");
            headerRowCount = headerRowList.Count;
            for (int i = 0; i < headerRowList.Count; i++)
            {
                bool borderBottom = false;
                if (i == headerRowList.Count - 1)
                {
                    borderBottom = true;
                }
                CreatePrepareCell(headerRowList, i, colCount, tableHeaderCellParaItem, true, borderBottom);
            }

            XmlNodeList bodyRowList = tableXmlNode.SelectNodes("./tbody/tr");

            for (int i = 0; i < bodyRowList.Count; i++)
            {
                CreatePrepareCell(bodyRowList, i, colCount, tableBodyCellParaItem, false, false, headerRowCount);
            }
        }

        private void CreatePrepareCell(XmlNodeList rowList, int i, int colCount, ElementItem tableCellParaItem, bool isHeader, bool borderBottom = false, int indexOffset = 0)
        {
            XmlNode tempNode = rowList[i];
            i = i + indexOffset;
            XmlNodeList colList = tempNode.SelectNodes("./td");
            for (int j = 0; j < colList.Count; j++)
            {

                PreparingCell preparingCell = new PreparingCell();
                rowCellsDic[i].Add(preparingCell);

                preparingCell.IsHeader = isHeader;

                XmlNode col = colList[j];
                var innerXml = col.InnerXml.Trim();
                if (col.ChildNodes.Count > 0 && col.LastChild.Name == "hr")
                {
                    preparingCell.BorderBottom = true;
                    innerXml = innerXml.Substring(0, innerXml.Length - col.LastChild.OuterXml.Length);
                }
                else
                {
                    preparingCell.BorderBottom = borderBottom;
                }

                var paraHandler = new ParagraphHandler(innerXml, tableCellParaItem, styleName, document);
                preparingCell.paragraph = paraHandler.Handle();
                preparingCell.InnerXml = innerXml;

                if (col.Attributes["rowspan"] != null)
                {
                    preparingCell.Rowspan = int.Parse(col.Attributes["rowspan"].Value);
                }
                else
                {
                    preparingCell.Rowspan = 1;
                }

                if (col.Attributes["colspan"] != null)
                {
                    preparingCell.Colspan = int.Parse(col.Attributes["colspan"].Value);
                }
                else
                {
                    preparingCell.Colspan = 1;
                }

                if (col.Attributes["align"] != null)
                {
                    if (col.Attributes["align"].Value == "center")
                    {
                        preparingCell.HorizontalAlignment = HorizontalAlignment.CENTER;
                    }
                }

                //计算单元格内容宽度
                var paraWidth = preparingCell.paragraph.GetWidthOnRendering(document);
                preparingCell.Width = paraWidth;

                int s = j;

                //针对跨列，已经设置过宽度，自动移位
                while (columnRowWidthDic[s][i] != -1f)
                {
                    s++;
                }
                if (s >= colCount)
                {
                    throw new Exception("The column index is out of bound.");
                }

                //针对跨行，已经设置过宽度，自动移位
                int t = i;
                while (columnRowWidthDic[s][t] != -1f)
                {
                    t++;
                }
                columnRowWidthDic[s][t] = paraWidth;

                preparingCell.ColumnIndex = s;
                preparingCell.RowIndex = t;

                //跨行情况，设置被跨单元格的宽度
                if (preparingCell.Rowspan > 1)
                {
                    for (int x = 1; x < preparingCell.Rowspan; x++)
                    {
                        columnRowWidthDic[s][t + x] = paraWidth;
                    }
                }
                //跨列的情况，所有单元格平分总长度
                if (preparingCell.Colspan > 1)
                {
                    for (int x = 0; x < preparingCell.Colspan; x++)
                    {
                        columnRowWidthDic[s + x][t] = paraWidth / float.Parse(preparingCell.Colspan.ToString());
                    }
                }
            }
        }

        private void InitializeColumnWidthArray(float tableWidth, bool rotation)
        {
            var maxTableWidth = GetTableMaxWidth();

            if (maxTableWidth > tableWidth && !rotation)
            {
                fixedColumnWidth = true;
                var columnCount = GetColumnCount();
                columnWidthArray = new float[columnCount];

                var calculatedTableWidth = 0f;

                var averageWidth = GetFloat(tableWidth / float.Parse(columnCount.ToString()));

                //未设置宽度的max width总合
                var unsetTotalMaxWidth = 0f;

                for (int i = 0; i < columnCount; i++)
                {
                    var columnRowWidth = columnRowWidthDic[i];
                    var columnMaxWidth = columnRowWidth.Max();
                    if (columnMaxWidth < averageWidth)
                    {
                        var thisColumnWidth = columnMaxWidth;
                        if (!rotation)
                        {
                            thisColumnWidth += 5f;
                        }
                        columnWidthArray[i] = GetFloat(thisColumnWidth);
                        calculatedTableWidth += columnWidthArray[i];
                    }
                    else
                    {

                        columnWidthArray[i] = 0f;
                        unsetTotalMaxWidth += columnMaxWidth;

                    }
                }
                var nonSetCount = columnWidthArray.Count(t => t == 0f);

                //超过2/3未设置，则平分宽度
                if (nonSetCount > float.Parse(columnWidthArray.Count().ToString()) * 3f / 2f)
                {
                    for (int i = 0; i < columnWidthArray.Length; i++)
                    {
                        columnWidthArray[i] = averageWidth;
                    }
                }
                else
                {
                    //剩余未设置宽度
                    var remainWidth = tableWidth - calculatedTableWidth;

                    for (int i = 0; i < columnCount; i++)
                    {
                        if (columnWidthArray[i] == 0f)
                        {
                            var columnMaxWidth = columnRowWidthDic[i].Max();
                            columnWidthArray[i] = GetFloat((remainWidth * (columnMaxWidth / unsetTotalMaxWidth)));
                        }
                    }
                }

                //如有剩余宽度，补齐
                var maxIndex = MaxIndex<float>(columnWidthArray);
                var exceptMaxWidth = 0f;
                for (int i = 0; i < columnWidthArray.Length; i++)
                {
                    if (i != maxIndex)
                    {
                        exceptMaxWidth += columnWidthArray[i];
                    }
                }
                columnWidthArray[maxIndex] = tableWidth - GetFloat(exceptMaxWidth);

            }
        }

        private float GetTableMaxWidth()
        {
            var tableWidth = 0f;
            foreach (var kvp in columnRowWidthDic)
            {
                tableWidth += kvp.Value.Max();
            }
            return tableWidth;
        }

        private bool IsTableRotation(float tableMaxWidth)
        {
            var pageWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - ElementGenerator.DocumentMargins[3] - ElementGenerator.DocumentMargins[1];

            if (tableMaxWidth > pageWidth * 3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private PreparingCell GetPreparingCell(int rowIndex, int columnIndex)
        {
            PreparingCell cell = null;
            foreach (var kvp in rowCellsDic)
            {
                cell = kvp.Value.FirstOrDefault(t => t.RowIndex == rowIndex && t.ColumnIndex == columnIndex);
                if (cell != null)
                {
                    break;
                }
            }
            return cell;
        }

        private float GetColumnHeight(int columnIndex)
        {
            var height = 0f;
            height = rowCellsDic[columnIndex].Sum(t => t.Width);

            return height;
        }

        //暂时无用
        private void SplitTable(int startRowIndex, bool addCaption, bool addHeader, float pageWidth)
        {
            var colCount = GetColumnCount();
            TableHandler tableHandler = null;
            if (fixedColumnWidth)
            {
                tableHandler = new TableHandler(sourceData, tableItem, styleName, UnitValue.CreatePointArray(columnWidthArray), false, document);
            }
            else
            {
                tableHandler = new TableHandler(sourceData, tableItem, styleName, colCount, false, document);
            }

            var table = tableHandler.Handle();

            if (addCaption)
            {
                captionItem.Colspan = colCount;
                captionItem.Rowspan = 1;

                var captionCellHandler = new TableCellHandler(sourceData, captionItem, styleName, document);
                var captionCell = captionCellHandler.Handle();
                table.AddCell(captionCell);

                var captionMarginItem = this.tableItem.Items.FirstOrDefault(t => t.Name == "TableCaptionMargin");
                if (captionMarginItem != null)
                {
                    captionMarginItem.Colspan = colCount;
                    captionMarginItem.Rowspan = 1;
                    var captionMarginCellHandler = new TableCellHandler(string.Empty, captionMarginItem, styleName, document);
                    var captionMarginCell = captionMarginCellHandler.Handle();
                    table.AddCell(captionMarginCell);
                }
            }
            if (addHeader)
            {
                bool isHeader = true;
                foreach (var kvp in rowCellsDic)
                {
                    foreach (var prepareCell in kvp.Value)
                    {
                        if (prepareCell.IsHeader)
                        {
                            var currentCellItem = tableHeaderItem;
                            currentCellItem.Rowspan = prepareCell.Rowspan;
                            currentCellItem.Colspan = prepareCell.Colspan;
                            if (prepareCell.BorderBottom)
                            {
                                currentCellItem.BorderBottom = headerCellBorder;
                            }
                            else
                            {
                                currentCellItem.BorderBottom = null;
                            }

                            var bodyCellHandler = new TableCellHandler(null, currentCellItem, styleName, document);
                            var bodyCell = bodyCellHandler.Handle();
                            bodyCell.Add(prepareCell.paragraph);
                            table.AddCell(bodyCell);
                        }
                        else
                        {
                            isHeader = false;
                            break;
                        }
                    }
                    if (!isHeader)
                    {
                        break;
                    }
                }
            }

            Dictionary<float[], int> cellRowSpan = new Dictionary<float[], int>();
            for (int i = startRowIndex; i < rowCellsDic.Count; i++)
            {
                foreach (var prepareCell in rowCellsDic[i])
                {
                    if (!prepareCell.IsHeader)
                    {
                        var currentCellItem = tableCellItem;

                        if (prepareCell.Rowspan > 1)
                        {
                            cellRowSpan.Add(new float[] { prepareCell.ColumnIndex, prepareCell.RowIndex }, prepareCell.Rowspan);
                        }



                        currentCellItem.Rowspan = 1;//prepareCell.Rowspan;
                        currentCellItem.Colspan = prepareCell.Colspan;
                        if (prepareCell.BorderBottom)
                        {
                            currentCellItem.BorderBottom = headerCellBorder;
                        }
                        else
                        {
                            currentCellItem.BorderBottom = null;
                        }
                        
                        currentCellItem.HorizontalAlignment = prepareCell.HorizontalAlignment.Value;
                        

                        var bodyCellHandler = new TableCellHandler(null, currentCellItem, styleName, document);
                        var bodyCell = bodyCellHandler.Handle();
                        bodyCell.Add(prepareCell.paragraph);
                        table.AddCell(bodyCell);
                    }

                    startRowIndex++;
                }
            }
        }

        //Source: https://blog.csdn.net/doubworm/java/article/details/103662454
        public static int MaxIndex<T>(T[] arr) where T : IComparable<T>
        {
            var i_Pos = 0;
            var value = arr[0];
            for (var i = 1; i < arr.Length; ++i)
            {
                var _value = arr[i];
                if (_value.CompareTo(value) > 0)
                {
                    value = _value;
                    i_Pos = i;
                }
            }
            return i_Pos;
        }

        public static float GetFloat(float value)
        {
            var roundValue = Math.Round(double.Parse(value.ToString()), 2);
            return float.Parse(roundValue.ToString());
        }

        private class PreparingCell
        {
            public Paragraph paragraph;
            public List List;
            public bool BorderBottom;
            public int Rowspan;
            public int Colspan;
            public bool IsHeader;
            public int RowIndex;
            public int ColumnIndex;
            public string InnerXml;
            public float Width;
            public HorizontalAlignment? HorizontalAlignment { get; set; }
        }
    }
}
