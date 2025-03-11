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
    public abstract class HtmlArticleTableBaseHandler<T>: ElementBaseHandler<T> where T : IElement
    {
        protected ElementItem captionItem;
        protected ElementItem graphicItem;
        protected ElementItem tableHeaderItem;
        protected ElementItem tableCellItem;
        protected ElementItem footNotesItem;

        protected ElementItem tableTableItem;

        protected BorderStyle headerCellBorder;
        protected bool borderDefinitedInSytleItem;

        protected Dictionary<int, float[]> columnRowWidthDic = new Dictionary<int, float[]>();

        protected float[] columnWidthArray;

        protected List<float> columnMaxWidthsArray;

        protected Dictionary<int, float> tableColumnMinWidths;

        protected bool fixedColumnWidth = false;

        protected Dictionary<int, List<PreparingCell>> rowCellsDic = new Dictionary<int, List<PreparingCell>>();

        protected XmlNode tableXmlNode;

        protected bool rotation;

        protected TableType? tableType;

        protected TableDisplayType? tableDisplayType;

        protected int headerRowCount;


        protected HtmlArticleTableBaseHandler(object sourceData, ElementItem item, string styleName, Document document)
            : base(sourceData, item, styleName, document)
        {

        }

        public HtmlArticleTableBaseHandler(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            string styleName, Document document, TableType? tableType, TableDisplayType? tableDisplayType) : this(sourceData,
                tableXmlNode, tableItem, null, styleName, document, tableType, tableDisplayType)
        {
            
        }

        public HtmlArticleTableBaseHandler(object sourceData,
            XmlNode tableXmlNode,
            ElementItem tableItem,
            BorderStyle headerCellBorder,
            string styleName, Document document,
            TableType? tableType, TableDisplayType? tableDisplayType)
            : base(sourceData, tableItem, styleName, document)
        {
            this.tableXmlNode = tableXmlNode;
            this.headerCellBorder = headerCellBorder;
            this.tableType = tableType;
            this.tableDisplayType = tableDisplayType;

            
        }

        public bool GetRotation()
        {
            return rotation;
        }

        public TableDisplayType GetDisplayType()
        {
            return this.tableDisplayType.Value;
        }

        protected int GetColumnCount()
        {
            int cols = 0;

            //只检测第一行
            foreach (XmlNode tempNode in tableXmlNode.SelectSingleNode("//tr").ChildNodes)
            {
                if (tempNode.NodeType == XmlNodeType.Element && (tempNode.Name == "td" || tempNode.Name == "th"))
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

        protected void CreateColumnMatrix()
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
                CreatePrepareCell(headerRowList, i, colCount, tableHeaderCellParaItem, true);
            }

            XmlNodeList bodyRowList = tableXmlNode.SelectNodes("./tbody/tr");

            for (int i = 0; i < bodyRowList.Count; i++)
            {
                CreatePrepareCell(bodyRowList, i, colCount, tableBodyCellParaItem, false, headerRowList.Count);
            }
        }

        protected void CreatePrepareCell(XmlNodeList rowList, int i, int colCount, ElementItem tableCellParaItem, bool isHeader, int indexOffset = 0)
        {
            XmlNode tempNode = rowList[i];
            i = i + indexOffset;
            XmlNodeList colList = tempNode.SelectNodes("./td");
            if (colList == null || colList.Count == 0)
            {
                colList = tempNode.SelectNodes("./th");
            }
            for (int j = 0; j < colList.Count; j++)
            {
                PreparingCell preparingCell = new PreparingCell();
                rowCellsDic[i].Add(preparingCell);

                preparingCell.IsHeader = isHeader;

                XmlNode col = colList[j];
                var innerXml = col.InnerXml;
#if DEBUG
                if (innerXml.Contains("Heterogeneity"))
                {

                }
#endif
                if (!borderDefinitedInSytleItem)
                {
                    if (col.ChildNodes.Count > 0 && col.LastChild.Name == "hr")
                    {
                        preparingCell.BorderBottom = true;
                        innerXml = innerXml.Substring(0, innerXml.Length - col.LastChild.OuterXml.Length);
                    }
                }

                tableCellParaItem.FirstLineIndent = 0f;

                if (!string.IsNullOrEmpty(innerXml))
                {
                    if (innerXml.StartsWith(" ") || innerXml.ToLower().StartsWith("<space"))
                    {
                        innerXml = innerXml.Trim();
                        tableCellParaItem.FirstLineIndent = 10f;
                    }
                }

                var paraWidth = 0f;
                if (string.IsNullOrEmpty(innerXml))
                {
                    preparingCell.paragraph = null;
                    paraWidth = 0f;
                }
                else
                {
                    if (innerXml.Contains("<p>") || innerXml.Contains("<list"))
                    {
                        preparingCell.Elements = new List<IBlockElement>();
                        var textStr = innerXml.Replace("> <", "><Space /><");
                        var textXml = "<Paragraph>" + TextHelper.InitXml(textStr) + "</Paragraph>";
                        XmlDocument contentDoc = new XmlDocument();
                        contentDoc.LoadXml(textXml);

                        var rootNode = contentDoc.ChildNodes[0];

                        foreach (XmlNode childNode in rootNode.ChildNodes)
                        {
                            if (childNode.Name == "list")
                            {
                                var listXml = childNode.OuterXml;
                                var list = CreateTableCellList(listXml);
                                preparingCell.Elements.Add(list);
                            }
                            else if (childNode.Name == "p")
                            {
                                if (childNode.InnerXml.Trim().StartsWith("<list"))
                                {
                                    var listXml = childNode.InnerXml;
                                    var list = CreateTableCellList(listXml);
                                    preparingCell.Elements.Add(list);
                                }
                                else
                                {
                                    var paraHandler = new ParagraphHandler(childNode.InnerXml, tableCellParaItem, styleName, document);
                                    var cellParagraph = paraHandler.Handle();

                                    preparingCell.Elements.Add(cellParagraph);
                                }
                            }
                        }

                        preparingCell.Elements[preparingCell.Elements.Count - 1].SetProperty(iText.Layout.Properties.Property.PADDING_BOTTOM, UnitValue.CreatePointValue(0f));
                        if (preparingCell.Elements[0] is Paragraph)
                        {
                            paraWidth = (preparingCell.Elements[0] as Paragraph).GetWidthOnRendering(document);
                        }
                        else
                        {
                            paraWidth = (preparingCell.Elements[0] as List).GetWidthOnRendering(document);
                        }
                        preparingCell.InnerXml = innerXml;
                    }
                    else
                    {
                        var paraHandler = new ParagraphHandler(innerXml, tableCellParaItem, styleName, document);
                        preparingCell.paragraph = paraHandler.Handle();
                        preparingCell.paragraph.SetPaddingBottom(0f);
                        preparingCell.InnerXml = innerXml;

                        //计算单元格内容宽度
                        paraWidth = preparingCell.paragraph.GetWidthOnRendering(document);
                    }

                    
                }



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
                        preparingCell.TextAlignment = TextAlignment.CENTER;
                    }
                }

                if (isHeader)
                {
                    if (preparingCell.Rowspan + i == rowList.Count)
                    {
                        preparingCell.BorderBottom = true;
                    }
                    else
                    {
                        if (col.Attributes["style"] != null)
                        {
                            var style = col.Attributes["style"].Value;
                            if (!string.IsNullOrEmpty(style))
                            {
                                var styleArray = style.Split(new char[] { ';' });
                                foreach (var styleItem in styleArray)
                                {
                                    if (styleItem.Contains("border-bottom"))
                                    {
                                        preparingCell.BorderBottom = true;
                                    }
                                    
                                }
                            }
                            
                        }
                    }
                }

                if (col.Attributes["style"] != null)
                {
                    var style = col.Attributes["style"].Value;
                    if (!string.IsNullOrEmpty(style))
                    {
                        var styleArray = style.Split(new char[] { ';' });
                        foreach (var styleItem in styleArray)
                        {
                            if (styleItem.Trim().StartsWith("background-color:"))
                            {
                                preparingCell.BackColor = styleItem.Trim().Substring(styleItem.IndexOf(":") + 1).Trim();
                            }
                        }
                    }

                }




                if (col.Attributes["width"] != null)
                {
                    preparingCell.Width = float.Parse(col.Attributes["width"].Value);
                }

                if (col.Attributes["cellwidth"] != null)
                {
                    preparingCell.Width = float.Parse(col.Attributes["cellwidth"].Value);
                }
                //else
                //{
                //    preparingCell.Width = paraWidth;
                //}

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

                ////跨行跨列的情况，设置被跨单元格的宽度
                //if (preparingCell.Rowspan > 1)
                //{
                //    for (int x = 1; x < preparingCell.Rowspan; x++)
                //    {
                //        columnRowWidthDic[s][t + x] = paraWidth;
                //    }
                //}
                //if (preparingCell.Colspan > 1)
                //{
                //    for (int x = 1; x < preparingCell.Colspan; x++)
                //    {
                //        columnRowWidthDic[s + x][t] = 0f;
                //    }
                //}

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

        protected abstract List CreateTableCellList(string innerXml);
        protected void InitializeColumnWidthArray(float tableWidth, bool rotation)
        {
            var maxTableWidth = GetTableMaxWidth();

            if (maxTableWidth > tableWidth)
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

        protected float GetTableMaxWidth()
        {
            var tableWidth = 0f;
            columnMaxWidthsArray = new List<float>();
            foreach (var kvp in columnRowWidthDic)
            {
                tableWidth += kvp.Value.Max();
                //columnMaxWidthsArray.Add(kvp.Value.Skip(headerRowCount).Max());
                columnMaxWidthsArray.Add(kvp.Value.Max());
            }
            return tableWidth;
        }

        protected bool IsTableRotation(float tableMaxWidth)
        {
            var pageWidth = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - document.GetRightMargin() - document.GetLeftMargin();

            if (GetColumnCount() > 5 && (GetColumnCount() > 12 || tableMaxWidth > pageWidth * 2.5f))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected PreparingCell GetPreparingCell(int rowIndex, int columnIndex)
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

        protected float GetColumnHeight(int columnIndex)
        {
            var height = 0f;
            height = rowCellsDic[columnIndex].Sum(t => t.Width);

            return height;
        }

        //https://blog.csdn.net/doubworm/java/article/details/103662454
        public static int MaxIndex<T1>(T1[] arr) where T1 : IComparable<T1>
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

        public static string GetInnerText(string innerXml)
        {
            var textStr = innerXml.Replace("> <", "><Space /><");
            var textXml = "<Paragraph>" + TextHelper.InitXml(textStr) + "</Paragraph>";
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(textXml);

            var rootNode = contentDoc.ChildNodes[0];
            return rootNode.InnerText;
        }

    }

    public class PreparingCell
    {
        public Paragraph paragraph { get; set; }

        public List List { get; set; }

        public List<Paragraph> Paragraphs { get; set; }

        public List<IBlockElement> Elements { get; set; }

        public bool BorderBottom { get; set; }

        public int Rowspan { get; set; }

        public int Colspan { get; set; }

        public bool IsHeader { get; set; }

        public int RowIndex { get; set; }

        public int ColumnIndex { get; set; }


        public string InnerXml { get; set; }

        public float Width { get; set; }

        public float MinWidth { get; set; }

        public HorizontalAlignment? HorizontalAlignment { get; set; }

        public TextAlignment? TextAlignment { get; set; }

        public string BackColor { get; set; }
    }
}
