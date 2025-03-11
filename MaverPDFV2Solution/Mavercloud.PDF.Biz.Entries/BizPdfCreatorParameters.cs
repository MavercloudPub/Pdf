
using Mavercloud.PDF.Config.Element;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz.Entries
{
    public class BizPdfCreatorParameters
    {
        public JInfo Journal { get; set; }

        public string StorageConnectionString { get; set; }

        public bool UserLocalDirectory { get; set; }

        public string FileOutputDir { get; set; }
        public string ZipFilePath { get; set; }

        public string PdfFileName { get; set; }

        public string FontEncoding { get; set; }

        public string ElementConfigurationXml { get; set; }

        public List<string> DecimalEntityList { get; set; }

        public string SpecialCharConfigXml { get; set; }

        public string JournalLogoPath { get; set; }

        public string QrCodePath { get; set; }

        public string ORCIDLogoPath { get; set; }

        public string Margins { get; set; }

        public float? TitleWordSpacing { get; set; }

        public float? TitleSpaceFontSize { get; set; }

        public float? TitleCharacterSpacing { get; set; }

        public float? AuthorSpaceFontSize { get; set; }
        public Dictionary<string, ArticleAddingTableStyleInfo> TablsStyles { get; set; }

        public Dictionary<string, ParagraphStyleInfo> ParagraphStyles { get; set; }

        public Dictionary<int, float> PageBottomMargins { get; set; }

        public Dictionary<string, float> InlineImageHeights { get; set; }

        public ArticleExInfo ArticleExInfo { get; set; }

        public Dictionary<string, ParagraphStyleInfo> ReferenceStyles { get; set; }

        public string WatermarkContent { get; set; }

    }

    public class ArticleExInfo
    {
        public int? StartPage { get; set; }

        public int? EndPage { get; set; }

        public string Correspondence { get; set; }

        public DateTime? PublishDate { get; set; }
    }
}
