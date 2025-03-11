using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Mavercloud.PDF.ElementHandler
{
    public static class ElementGenerator
    {
        internal static float[] DocumentMargins;

        public static void SetDocumentMargins(string margins)
        {
            if (DocumentMargins == null)
            {
                var marginConfigArray = margins.Split(new char[] { ',' });
                if (marginConfigArray.Length == 4)
                {
                    DocumentMargins = new float[4];
                    DocumentMargins[0] = float.Parse(marginConfigArray[0]);
                    DocumentMargins[1] = float.Parse(marginConfigArray[1]);
                    DocumentMargins[2] = float.Parse(marginConfigArray[2]);
                    DocumentMargins[3] = float.Parse(marginConfigArray[3]);
                }
            }

        }
        public static void AddElement(object sourceData, ElementItem item, Document document, string styleName)
        {
            switch (item.Type)
            {
                case ElementType.Table:
                case ElementType.Paragraph:
                case ElementType.Div:
                case ElementType.EmptyItem:
                    var element = GetBlockElement(sourceData, item, document, styleName);
                    if (element != null)
                    {
                        document.Add(element);
                    }
                    break;
                default:
                    break;
            }
        }

        public static IBlockElement AddBlockElement(object sourceData, ElementItem item, Document document, string styleName)
        {
            var element = GetBlockElement(sourceData, item, document, styleName);
            if (element != null)
            {
                document.Add(element);
            }
            return element;
        }

        public static IBlockElement GetBlockElement(object sourceData, ElementItem item, Document document, string styleName)
        {
            IBlockElement element = null;
            var createElement = true;
            if (!string.IsNullOrEmpty(item.BasedValuePath))
            {
                var basedValueObject = Mavercloud.PDF.Helpers.Object.GetFollowingPropertyValue(item.BasedValuePath, sourceData);
                if (basedValueObject == null)
                {
                    createElement = false;
                }
            }
            if (createElement)
            {
                switch (item.Type)
                {
                    case ElementType.Paragraph:
                        var paraHandler = new ParagraphHandler(sourceData, item, styleName, document);
                        element = paraHandler.Handle();
                        break;
                    case ElementType.Table:
                        var tableHandler = new TableHandler(sourceData, item, styleName, document);
                        element = tableHandler.Handle();
                        break;
                    case ElementType.Div:
                        var divHandler = new DivHandler(sourceData, item, styleName, document);
                        element = divHandler.Handle();
                        break;
                    case ElementType.EmptyItem:
                        element = new Table(1).SetHeight(item.Height.Value).SetBorder(Border.NO_BORDER)
                            .AddCell(new Cell().Add(new Paragraph(string.Empty).SetPadding(0)).SetPadding(0).SetBorder(Border.NO_BORDER))
                            .SetPadding(0)
                            .SetMargin(0);
                        if (item.BorderMargin != null)
                        {
                            element.SetNextRenderer(new BorderMarginTableRenderer(element as Table, item.BorderMargin));
                        }
                        break;
                    case ElementType.List:
                        var listHanlder = new ListHandler(sourceData, item, styleName, document);
                        element = listHanlder.Handle();
                        break;
                    default:
                        break;
                }
            }
            return element;
        }

        public static Table GetTable(object sourceData, ElementItem item, Document document, string styleName, UnitValue[] columnWidth)
        {
            var tableHandler = new TableHandler(sourceData, item, styleName, columnWidth, document);
            var table = tableHandler.Handle();
            return table;
        }

        public static Table GetTable(object sourceData, ElementItem item, Document document, string styleName, int? columnCount)
        {
            var tableHandler = new TableHandler(sourceData, item, styleName, columnCount, document);
            var table = tableHandler.Handle();
            return table;
        }

        public static Table GetTable(object sourceData, ElementItem item, Document document, string styleName)
        {
            var tableHandler = new TableHandler(sourceData, item, styleName, document);
            var table = tableHandler.Handle();
            return table;
        }

        public static Table AddTable(object sourceData, ElementItem item, Document document, string styleName)
        {
            var table = GetTable(sourceData, item, document, styleName);
            document.Add(table);
            return table;
        }

        public static Table AddTable(object sourceData, ElementItem item, Document document, string styleName, int? columnCount)
        {
            var table = GetTable(sourceData, item, document, styleName, columnCount);
            document.Add(table);
            return table;
        }
        public static Table AddTable(object sourceData, ElementItem item, Document document, string styleName, UnitValue[] columnWidth)
        {
            var table = GetTable(sourceData, item, document, styleName, columnWidth);
            document.Add(table);
            return table;
        }


        public static Border GetBorder(BorderStyle elementBorder)
        {
            Border border = null;
            if (elementBorder.Type != BorderType.None)
            {
                switch (elementBorder.Type)
                {
                    case BorderType.Solid:
                        border = new SolidBorder(elementBorder.Width.Value);
                        break;
                    default:
                        border = new SolidBorder(elementBorder.Width.Value);
                        break;
                }
                if (border != null)
                {
                    if (!string.IsNullOrEmpty(elementBorder.Color))
                    {
                        border.SetColor(GetColor(elementBorder.Color));
                    }
                }
            }
            return border;
        }

        public static Color GetColor(string nameOrRgbColor)
        {
            Color color = null;
            if (nameOrRgbColor.Contains(","))
            {
                var colorArray = nameOrRgbColor.Split(new char[] { ',' });
                if (colorArray.Length == 3)
                    color = new DeviceRgb(int.Parse(colorArray[0]), int.Parse(colorArray[1]), int.Parse(colorArray[2]));
            }
            else if (nameOrRgbColor.StartsWith("#"))
            {
                var colorArray = HexToRgb(nameOrRgbColor);
                if (colorArray.Length == 3)
                    color = new DeviceRgb(colorArray[0], colorArray[1], colorArray[2]);

            }
            else
            {
                switch (nameOrRgbColor.ToUpper())
                {
                    case "GRAY":
                        color = iText.Kernel.Colors.ColorConstants.GRAY;
                        break;
                    case "BLACK":
                        color = iText.Kernel.Colors.ColorConstants.BLACK;
                        break;
                    case "WHITE":
                        color = iText.Kernel.Colors.ColorConstants.WHITE;
                        break;
                    case "RED":
                        color = iText.Kernel.Colors.ColorConstants.RED;
                        break;
                    case "YELLOW":
                        color = iText.Kernel.Colors.ColorConstants.YELLOW;
                        break;
                    case "GREEN":
                        color = iText.Kernel.Colors.ColorConstants.GREEN;
                        break;
                    default:
                        color = iText.Kernel.Colors.ColorConstants.BLACK;
                        break;
                }
            }
            return color;
        }

        private static int[] HexToRgb(string hexColor)
        {
            // Remove the "#" symbol if present
            hexColor = hexColor.TrimStart('#');

            // Parse the individual R, G, B components from Hex to decimal
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);

            return new int[] { r, g, b };
        }

        public static void AddElementToCanvas(IBlockElement element, Rectangle rectangle, PdfDocument pdfDocument)
        {
            var page = pdfDocument.GetLastPage();
            var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
            pdfCanvas.Rectangle(rectangle).SetLineWidth(0f);
            var canvas = new Canvas(pdfCanvas, rectangle);
            canvas.Add(element)
                .SetBorder(Border.NO_BORDER)
                .SetStrokeWidth(0f)
                .Close();
            pdfCanvas.Release();
        }

        public static void AddImageToCanvas(Image element, Rectangle rectangle, PdfDocument pdfDocument)
        {
            var page = pdfDocument.GetLastPage();
            AddImageToCanvas(element, rectangle, pdfDocument, page);
        }

        public static void AddImageToCanvas(Image element, Rectangle rectangle, PdfDocument pdfDocument, PdfPage page)
        {
            var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
            pdfCanvas.Rectangle(rectangle).SetLineWidth(0);

            var canvas = new Canvas(pdfCanvas, rectangle);
            canvas.Add(element)
                .SetBorder(Border.NO_BORDER)
                .SetStrokeWidth(0f)
                .Close();
            pdfCanvas.Release();
        }
    }
}
