using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Layout;
using iText.Layout.Renderer;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mavercloud.PDF
{
    public class PDFHelpers
    {
        public static string UnBreakSpace = "\u00A0";
        //public static AzureStorageServiceBase storageService;
        public static void UnembedTTF(PdfDocument pdfDocument)
        {
            for (int i = 0; i < pdfDocument.GetNumberOfPdfObjects(); i++)
            {
                PdfObject obj = pdfDocument.GetPdfObject(i);

                // Skip all objects that aren't a dictionary
                if (obj == null || !obj.IsDictionary())
                {
                    continue;
                }

                // Process all dictionaries
                UnembedTTF((PdfDictionary)obj);
            }
        }
        /*
         * Unembeds a font dictionary.
         */
        public static void UnembedTTF(PdfDictionary dict)
        {
            // Ignore all dictionaries that aren't font dictionaries
            if (!PdfName.Font.Equals(dict.GetAsName(PdfName.Type)))
            {
                return;
            }

            // Only TTF fonts should be removed
            if (dict.GetAsDictionary(PdfName.FontFile2) != null)
            {
                return;
            }

            // Check if a subset was used (in which case we remove the prefix)
            PdfName baseFont = dict.GetAsName(PdfName.BaseFont);
            var byteArray = Encoding.UTF8.GetBytes(baseFont.GetValue());
            if (byteArray.Length >= 7 && byteArray[6] == '+')
            {
                baseFont = new PdfName(baseFont.GetValue().Substring(7));
                dict.Put(PdfName.BaseFont, baseFont);
            }

            // Check if there's a font descriptor
            PdfDictionary fontDescriptor = dict.GetAsDictionary(PdfName.FontDescriptor);
            if (fontDescriptor == null)
            {
                return;
            }

            // Replace the fontname and remove the font file
            fontDescriptor.Put(PdfName.FontName, baseFont);
            fontDescriptor.Remove(PdfName.FontFile2);
        }

        public static void DrawBackShading(DrawContext drawContext, Rectangle rectangle, AxialShadingStyle shadingStyle)
        {
            var startColor = ElementGenerator.GetColor(shadingStyle.StartColor).GetColorValue();
            var endColor = ElementGenerator.GetColor(shadingStyle.EndColor).GetColorValue();

            PdfShading.Axial axialShading = null;

            if (shadingStyle.Rotation.HasValue && shadingStyle.Rotation.Value)
            {
                axialShading = new PdfShading.Axial(new PdfDeviceCs.Rgb(),
                   rectangle.GetLeft(), rectangle.GetBottom(),
                   startColor,
                   rectangle.GetLeft(),
                   rectangle.GetBottom() + rectangle.GetHeight(),
                   endColor);
            }
            else
            {
                axialShading = new PdfShading.Axial(new PdfDeviceCs.Rgb(),
                   rectangle.GetLeft(), 0,
                   startColor,
                   rectangle.GetLeft() + rectangle.GetWidth(),
                   0,
                   endColor);
            }

            drawContext.GetCanvas()
                .SaveState()
                .SetFillColorShading(new PdfPattern.Shading(axialShading))
                .Rectangle(rectangle)
                .Fill()
                .RestoreState();
        }

        public static void ModifyAllFontSize(ElementItem item, float fontSize)
        {
            if (item.FontSize.HasValue && !item.TextRise.HasValue)
            {
                item.FontSize = fontSize;
            }
            if (item.Items != null)
            {
                foreach (var subItem in item.Items)
                {
                    ModifyAllFontSize(subItem, fontSize);
                }
            }
            if (item.Tags != null)
            {
                foreach (var tag in item.Tags)
                {
                    ModifyAllFontSize(tag, fontSize);
                }
            }
        }

        public static void ModifyAllFontSize(ChunkTag tag, float fontSize)
        {
            if (tag.FontSize.HasValue && !tag.TextRise.HasValue)
            {
                tag.FontSize = fontSize;
            }
            if (tag.Tags != null)
            {
                foreach (var subTag in tag.Tags)
                {
                    ModifyAllFontSize(subTag, fontSize);
                }
            }
        }

        
    }
}
