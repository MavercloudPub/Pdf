using iText.IO.Font;
using iText.Kernel.Font;
using iText.Layout;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Mavercloud.PDF
{
    public static class PDFFontHelper
    {
        private static ConcurrentDictionary<string, Dictionary<string, PdfFont>> docFonts = new ConcurrentDictionary<string, Dictionary<string, PdfFont>>();
        private static string _fontEncoding;


        public static void RegisterDirectory(string dir, string fontEncoding)
        {
            iText.Kernel.Font.PdfFontFactory.RegisterDirectory(dir);
            _fontEncoding = fontEncoding;


        }

        public static PdfFont GetFont(Document document, string fontName)
        {
            var docId = document.GetPdfDocument().GetOriginalDocumentId().GetValue();

            return GetFont(docId, fontName);
        }

        public static PdfFont GetFont(string docId, string fontName)
        {

            if (!docFonts.ContainsKey(docId))
            {
                docFonts.TryAdd(docId, new Dictionary<string, PdfFont>());
            }

            var fontsDictionary = docFonts[docId];

            if (fontsDictionary.ContainsKey(fontName))
            {
                return fontsDictionary[fontName];
            }
            else
            {
                if (string.IsNullOrEmpty(_fontEncoding))
                {
                    _fontEncoding = PdfEncodings.IDENTITY_H;
                }
                var font = PdfFontFactory.CreateRegisteredFont(fontName, _fontEncoding, PdfFontFactory.EmbeddingStrategy.PREFER_NOT_EMBEDDED);
                //if (fontName == "Times New Roman")
                //{
                //    PdfFontFactory.CreateRegisteredFont(fontName, PdfEncodings.WINANSI, true);
                //}
                fontsDictionary.Add(fontName, font);
                return font;
            }
        }

        public static bool RemoveCachedFonts(string docId)
        {
            Dictionary<string, PdfFont> fontDirectory = null;
            if (docFonts.TryRemove(docId, out fontDirectory))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
