
using Mavercloud.PDF.General.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.Biz.Entries
{
    public class ArticleInfo
    {
        public string Doi { get; set; }

        public string DoiUrl { get; set; }

        public string RunningTitleInFooter { get; set; }

        public string PageText { get; set; }

        public string JournalName { get; set; }

        public string JournalNameFormated { get; set; }

        public string JournalShortName { get; set; }

        public string OpenAccessText { get; set; }

        public string ISSN { get; set; }

        public int Year { get; set; }

        public string Volume { get; set; }

        public string ElocationId { get; set; }

        public bool? ContinuousPublish { get; set; }

        public string Subject { get; set; }

        public string ArticleTitle { get; set; }

        public string ArticleTitleText { get; set; }

        public string Authors { get; set; }

        public string AuthorsText { get; set; }

        public List<string> AuthorAffliations { get; set; }

        public List<string> AuthorNotes { get; set; }

        public string CorrespondenceTo { get; set; }

        public string License { get; set; }

        public string PublisherNote { get; set; }

        public string Citation { get; set; }

        public string DateContent { get; set; }

        public string EditorContent { get; set; }

        public Dictionary<string, string> AbstractContents { get; set; }

        public string QrCode { get; set; }

        public string Website { get; set; }

        public string Logo { get; set; }

        public string CheckUpdate { get; set; }

        public string LicenseLogo { get; set; }

        public string Keywords { get; set; }

        public int? StartPage { get; set; }

        /// <summary>
        /// 文章正文
        /// 包括Section
        /// </summary>
        public List<SectionInfo> ContentItems
        {
            get;
            set;
        }

        public List<SectionInfo> BackItems { get; set; }

        public List<Reference> Refs { get; set; }
    }
}
