using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Mavercloud.JATS.Core;
using Mavercloud.JATS.Entities;
using Mavercloud.PDF.Config.Element;
using Mavercloud.PDF.ElementHandler;
using Mavercloud.PDF.Elements;
using Mavercloud.PDF.EventHandler;
using Mavercloud.PDF.General.Element;
using Mavercloud.PDF.General.ElementHandler;
using Mavercloud.PDF.General.Entities;
using Mavercloud.PDF.General.Helpers;
using Mavercloud.PDF.General.Pages;
using Mavercloud.PDF.General.Renders;
using Mavercloud.PDF.Layout;
using Mavercloud.PDF.Biz.ElementHandler;
using Mavercloud.PDF.Biz.Entries;
using Mavercloud.PDF.Biz.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Mavercloud.PDF.Biz
{
    public class BizPdfCreator : IDisposable
    {
        private static List<string> nextIncludedcharacters = new List<string>() { ").", ".", ",", ";", "?" };

        private static List<string> previousincludedTag = new List<string>() { "italic", "Italic", "i" };

        private BizPdfCreatorParameters parameters;

        private DocumentItems layoutItems;

        private Dictionary<string, ArticleAddingTableStyleInfo> tableStyles;

        private string styleName;

        private ArticleInfo articleInfo;

        private Article article;

        private PdfWriter pdfWriter;

        private PdfDocument pdfDocument;

        private Document document;

        private Guid fileId { get; set; }

        private static Dictionary<string, string> htmlEscapeDecimal = new Dictionary<string, string>();

        private AzureStorageServiceBase storage;

        private bool isImageInDirectory;

        private string referenceIdPrefix;

        private ArticlePageStartEventHandler headerEventHandler;

        private ArticlePageEndEventHandler articlePageEndEventHanlder;

        private ArticlePageMarginEventHandler pageMarginEventHandler;


        private WaterMarkEventHandler waterMarkEventHandler;



        private ElementItem sectionTitleItem;
        private ElementItem sectionParaItem;
        private ElementItem sectionListItem;
        private ElementItem sectionFigureItem;
        private ElementItem sectionFigureCaptionItem;
        private ElementItem sectionTableItem;
        private ElementItem sectionEquationParaItem;
        private ElementItem sectionEquationItem;

        private ElementItem tableTableItem;
        private ElementItem tableHeaderItem;
        private ElementItem tableHeaderParaItem;
        private ElementItem tableCellItem;
        private ElementItem tableBodyCellParaItem;
        private ElementItem tableBodyCellListItem;

        private ElementItem licenseAndPublisherNoteItem;

        private float? defaultTableHeaderCellParagraphFontSize;
        private float? defaultTableBodyCellParagraphPaddingBottom;
        private float? defaultTableBodyCellParagraphFontSize;
        private float? defaultTableBodyCellParagraphMultileading;
        private bool? defaultShortFigureCaptionAlignCenter;

        private List<ArticleAddingTable> addingTables;

        private List<ArticleElement> articleElements;
        private List<ArticleElement> backElements;

        private List<string> toDrawTableIds;

        private float pageWidth;


        //页面可用高度
        private float pageHeight;

        private float pageRotationWidth;

        private float pageRotationHeight;

        //页面整体高度
        private float pageHeightWithMargin;

        //页面整体宽度
        private float pageWidthWithMargin;

        private float currentYLine;

        private int currentPageNumber;


        public bool startNewPageForLimitedSpace;

        private bool setFirstLineIndent = true;

        public bool pageEndEventFired = false;

        private List<PdfOutlineItem> outlineItems;

        private FigureInfo abstractGraphicInfo;

        private bool abstractGraphicDrawn;

        public BizPdfCreator(BizPdfCreatorParameters parameters)
        {
            outlineItems = new List<PdfOutlineItem>();
            this.parameters = parameters;

            if (!parameters.UserLocalDirectory && !string.IsNullOrEmpty(parameters.StorageConnectionString))
            {
                storage = new AzureStorageServiceBase(parameters.StorageConnectionString);
            }

            if (parameters.TablsStyles != null)
            {
                tableStyles = new Dictionary<string, ArticleAddingTableStyleInfo>();
            }


            if (htmlEscapeDecimal.Count <= 0 && parameters.DecimalEntityList != null)
            {

                htmlEscapeDecimal.Clear();
                foreach (string escape in parameters.DecimalEntityList)
                {
                    string[] escapeWithDecimal = escape.Split(new char[] { '\t' });
                    htmlEscapeDecimal.Add(escapeWithDecimal[1].Trim(), escapeWithDecimal[0].Trim());
                }
            }

            this.layoutItems = Helpers.Xml.XmlStrToObject<DocumentItems>(parameters.ElementConfigurationXml);

            if (!string.IsNullOrEmpty(parameters.ZipFilePath))
            {
                if (parameters.UserLocalDirectory)
                {
                    using (var stream = new FileStream(parameters.ZipFilePath, FileMode.Open))
                    {
                        Compress.Uncompress(stream, parameters.FileOutputDir);
                    }
                }
                else
                {
                    using (var stream = new MemoryStream())
                    {
                        storage.DownloadFileToStream(parameters.ZipFilePath, stream);
                        if (stream.CanSeek)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        Compress.Uncompress(stream, parameters.FileOutputDir);
                    }
                }
                if (string.IsNullOrEmpty(parameters.QrCodePath))
                {
                    parameters.QrCodePath = Directory.GetFiles(parameters.FileOutputDir, "qrcode*", SearchOption.AllDirectories).FirstOrDefault();
                }
            }

            isImageInDirectory = true;

            var xmlFile = Directory.GetFiles(parameters.FileOutputDir, "*.xml", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrEmpty(xmlFile))
            {
                var jATSAnalyzer = new JATSAnalyzer();
                var xml = File.ReadAllText(xmlFile, Encoding.UTF8);
                xml = PrepareArticleXml(xml);
                var inlineImages = Directory.GetFiles(parameters.FileOutputDir, "*.jpg", SearchOption.AllDirectories).Where(t => t.Contains(".in.")).ToList();
                if (inlineImages != null && inlineImages.Count > 0)
                {
                    foreach (var inlineImageFile in inlineImages)
                    {
                        var inlineImageHeight = 8.5f;
                        if (parameters.InlineImageHeights != null && parameters.InlineImageHeights.ContainsKey(System.IO.Path.GetFileNameWithoutExtension(inlineImageFile)))
                        {
                            inlineImageHeight = parameters.InlineImageHeights[System.IO.Path.GetFileNameWithoutExtension(inlineImageFile)];
                        }

                        xml = xml.Replace(System.IO.Path.GetFileName(inlineImageFile).Replace(".jpg", ".tif") + "\"", inlineImageFile + "\" Height=\"" + inlineImageHeight + "\"");
                        
                    }
                }

                article = jATSAnalyzer.GetArticle(xml);

            }
            else
            {
                throw new Exception("Xml File Not Found");
            }
        }

        public string CreateAndSave()
        {
            var pdfData = Create();

            if (!parameters.UserLocalDirectory)
            {
                var containerName = Guid.NewGuid().ToString();

                var fileUri = storage.UploadFile(containerName, parameters.PdfFileName, pdfData, true);
                return fileUri;
            }
            else
            {
                using (var fileStream = new FileStream(System.IO.Path.Combine(parameters.FileOutputDir, parameters.PdfFileName), FileMode.Create))
                {
                    fileStream.Write(pdfData, 0, pdfData.Length);
                }
                return System.IO.Path.Combine(parameters.FileOutputDir, parameters.PdfFileName);
            }

        }

        public byte[] Create()
        {
            InitArticleInfo();
            articleInfo.OpenAccessText = Constants.OpenAccess;
            articleInfo.Logo = parameters.JournalLogoPath;
            articleInfo.QrCode = parameters.QrCodePath;

            

            fileId = Guid.NewGuid();

            

            sectionTitleItem = layoutItems.Items.Single(t => t.Name == "SectionTitle");
            sectionParaItem = layoutItems.Items.Single(t => t.Name == "SectionParagraph");
            sectionListItem = layoutItems.Items.Single(t => t.Name == "SectionList");
            sectionEquationParaItem = layoutItems.Items.Single(t => t.Name == "SectionEquationParagraph");
            sectionEquationItem = layoutItems.Items.Single(t => t.Name == "SectionEquation");
            sectionFigureItem = layoutItems.Items.Single(t => t.Name == "SectionFigure");
            sectionFigureCaptionItem = layoutItems.Items.Single(t => t.Name == "SectionFigureCaption");
            sectionTableItem = layoutItems.Items.Single(t => t.Name == "SectionTableDiv");

            tableTableItem = sectionTableItem.Items.First(t => t.Name == "SectionTableTable");
            tableHeaderItem = tableTableItem.Items.First(t => t.Name == "TableHeader");
            tableHeaderParaItem = tableHeaderItem.Items.First(t => t.Type == ElementType.Paragraph);
            tableCellItem = tableTableItem.Items.First(t => t.Name == "TableContent");
            tableBodyCellParaItem = tableCellItem.Items.First(t => t.Type == ElementType.Paragraph);
            tableBodyCellListItem = tableTableItem.Items.First(t => t.Name == "TableContentList"); 

            licenseAndPublisherNoteItem = layoutItems.Items.Single(t => t.Name == "LicenseAndQrCodeAndPublisherNote");

            defaultTableHeaderCellParagraphFontSize = tableHeaderParaItem.FontSize;
            defaultTableBodyCellParagraphPaddingBottom = tableBodyCellParaItem.PaddingBottom;
            defaultTableBodyCellParagraphFontSize = tableBodyCellParaItem.FontSize;
            defaultTableBodyCellParagraphMultileading = tableBodyCellParaItem.MultipliedLeading;

            defaultShortFigureCaptionAlignCenter = sectionFigureCaptionItem.Items.First(t => t.Name == "FigureCaption").Items[0].TextAlignmentCenterForShortContent.GetValueOrDefault();

            var articleTitleItem = layoutItems.Items.Single(t => t.Name == "ArticleTitle");
            var authorItem = layoutItems.Items.Single(t => t.Name == "Authors");
            if (this.parameters.TitleWordSpacing.HasValue)
            { 
                articleTitleItem.WordSpacing = this.parameters.TitleWordSpacing.Value;
            }
            if (this.parameters.TitleSpaceFontSize.HasValue)
            {
                articleTitleItem.SpaceFontSize = this.parameters.TitleSpaceFontSize.Value;
            }
            if (this.parameters.TitleCharacterSpacing.HasValue)
            {
                articleTitleItem.CharacterSpacing = this.parameters.TitleCharacterSpacing.Value;
            }
            if (this.parameters.AuthorSpaceFontSize.HasValue)
            {
                authorItem.SpaceFontSize = this.parameters.AuthorSpaceFontSize.Value;
            }

            

            var writerProperties = new WriterProperties();
            writerProperties.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            writerProperties.SetFullCompressionMode(true);
            writerProperties.SetPdfVersion(PdfVersion.PDF_1_6);
            writerProperties.SetInitialDocumentId(new PdfString(fileId.ToString()));

            using (var pdfStream = new MemoryStream())
            {
                pdfWriter = new PdfWriter(pdfStream, writerProperties);

                var documentProperties = new DocumentProperties();
                pdfDocument = new PdfDocument(pdfWriter);
                pdfDocument.SetDefaultPageSize(Constants.DefaultPageSize);

                InitializeDocumentInfo();

                document = new Document(pdfDocument, Constants.DefaultPageSize);
                document.SetMargins(Constants.DocumentMargins[0], Constants.DocumentMargins[1], Constants.DocumentMargins[2], Constants.DocumentMargins[3]);
                if (parameters.PageBottomMargins != null && parameters.PageBottomMargins.ContainsKey(1))
                {
                    document.SetBottomMargin(parameters.PageBottomMargins[1]);
                }
                else
                {
                    document.SetBottomMargin(Constants.FirstPageBottomMargin);
                }


                pageHeightWithMargin = document.GetPdfDocument().GetDefaultPageSize().GetHeight();
                pageWidthWithMargin = document.GetPdfDocument().GetDefaultPageSize().GetWidth();

                pageWidth = pageWidthWithMargin - Constants.DocumentMargins[1] - Constants.DocumentMargins[3];
                pageHeight = pageHeightWithMargin - Constants.DocumentMargins[0] - Constants.DocumentMargins[2];

                pageRotationWidth = pageHeightWithMargin - Constants.DocumentMargins[2] - Constants.DocumentMargins[3];
                pageRotationHeight = pageWidthWithMargin - Constants.DocumentMargins[0] - Constants.DocumentMargins[2];

                PrepareHandler();
                
                headerEventHandler.SetEnabled(true);
                articlePageEndEventHanlder.SetEnabled(true);
                pageMarginEventHandler.SetEnabled(true);


                foreach (var item in layoutItems.Items.Where(t => t.Location == "FirstPage").ToList())
                {
                    var elementInFirstPage = ElementGenerator.GetBlockElement(articleInfo, item, document, string.Empty);
                    
                    if (elementInFirstPage != null)
                    {
                        document.Add(elementInFirstPage);
                    }
                }

                var abstractTitleItem = layoutItems.Items.First(t => t.Name == "AbstractTitle");
                var keywordsTitleItem = layoutItems.Items.First(t => t.Name == "KeywordsTitle");
                var keywordsItem = layoutItems.Items.First(t => t.Name == "Keywords");
                float remainSpace = 0;
                if (articleInfo.AbstractContents != null && articleInfo.AbstractContents.Count > 0)
                {
                    var abstractTtileElement = ElementGenerator.GetBlockElement(articleInfo, abstractTitleItem, document, null);
                    abstractTtileElement = AddTitleDestinationAndOutline("abstract", "Abstract", abstractTtileElement as iText.Layout.Element.Paragraph);
                    document.Add(abstractTtileElement);

                    var abstractContentItem = layoutItems.Items.First(t => t.Name == "AbstractContent");

                    for (int i = 0; i < articleInfo.AbstractContents.Count; i++)
                    {
                        var abstractContent = articleInfo.AbstractContents.ElementAt(i);
                        var clonedItem = abstractContentItem;
                        if (parameters.ParagraphStyles != null
                        && parameters.ParagraphStyles.ContainsKey(abstractContent.Key))
                        {
                            var paragraphStyle = parameters.ParagraphStyles[abstractContent.Key];
                            clonedItem = StyleHelper.GetElementItemByParagraphStyle(abstractContentItem, paragraphStyle);
                        }
                        var abstractContentElement = ElementGenerator.GetBlockElement(abstractContent.Value, clonedItem, document, null) as iText.Layout.Element.Paragraph;
                        if (i == articleInfo.AbstractContents.Count - 1)
                        {
                            abstractContentElement.SetPaddingBottom(13f);
                        }
                        document.Add(abstractContentElement);
                    }
                    currentPageNumber = document.GetRenderer().GetCurrentArea().GetPageNumber();
                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                    remainSpace = currentYLine - document.GetBottomMargin();
                    if ((remainSpace < 60f || currentPageNumber > 1) && abstractGraphicInfo != null)
                    {
                        DrawAbstractGraphic();
                    }

                }
                var pageNumber = 0;
                if (!string.IsNullOrWhiteSpace(articleInfo.Keywords))
                {
                    var keywordsTitleElement = ElementGenerator.GetBlockElement(articleInfo, keywordsTitleItem, document, null) as iText.Layout.Element.Paragraph;
                    keywordsTitleElement = AddTitleDestinationAndOutline("keywords", "Keywords", keywordsTitleElement);

                    headerEventHandler.SetCurrentParagraph(keywordsTitleElement);
                    document.Add(keywordsTitleElement);


                    var keywordsElement = ElementGenerator.GetBlockElement(articleInfo.Keywords, keywordsItem, document, null) as iText.Layout.Element.Paragraph;
                    document.Add(keywordsElement);
                }

                currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                remainSpace = currentYLine - document.GetBottomMargin();
                if (remainSpace > 20f && remainSpace < 55f)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    if (!abstractGraphicDrawn && abstractGraphicInfo != null)
                    {
                        DrawAbstractGraphic();
                    }
                }
                else if (remainSpace <= 20f)
                {
                    if (!abstractGraphicDrawn && abstractGraphicInfo != null)
                    {
                        DrawAbstractGraphic();
                    }
                }
                //初始化表格图片
                InitializeAddingTables();

                //初始化正文内容
                InitializeArticleElements();

                InitializeBackElements();


                DrawArticleElements(articleElements);

                DrawRemainTables();

                currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                if (backElements.Count > 0 || (articleInfo.Refs != null && articleInfo.Refs.Count > 0))
                {
                    if (currentYLine - Constants.DocumentMargins[2] > 25f && currentYLine - Constants.DocumentMargins[2] < 60f)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                    }
                }


                DrawArticleElements(backElements, true, true);
                

                if (articleInfo.Refs != null && articleInfo.Refs.Count > 0)
                {
                    //Reference
                    sectionTitleItem = layoutItems.Items.Single(t => t.Name == "SectionTitle").Items.First(t => t.Name == "SectionTitle1");
                    var refTitleElement = ElementGenerator.GetBlockElement("References", sectionTitleItem, document, styleName) as iText.Layout.Element.Paragraph;
                    refTitleElement = AddTitleDestinationAndOutline("references", "References", refTitleElement);
                    document.Add(refTitleElement);

                    var referenceListItem = layoutItems.Items.First(t => t.Name == "ReferenceList");
                    var refListHanlder = new BizReferenceListHandler(articleInfo, 
                        referenceListItem, 
                        styleName, 
                        document, 
                        parameters.ReferenceStyles);
                    var refList = refListHanlder.Handle();
                    
                    document.Add(refList);
                }

                

                if (outlineItems != null && outlineItems.Count > 0)
                {
                    PdfOutline root = pdfDocument.GetOutlines(false);
                    foreach (var item in outlineItems)
                    {
                        AddOutlines(item, root);
                    }
                }

                document.Close();

                return pdfStream.ToArray();
            }
        }

        private void DrawAbstractGraphic()
        {
            var abstractFigureItem = layoutItems.Items.First(t => t.Name == "AbstractFigure");

            if (abstractGraphicInfo.Captions != null && abstractGraphicInfo.Captions.Count == 1)
            {
                abstractFigureItem.Items.First(t => t.Name == "FigureCaption").Items[0].TextAlignmentCenterForShortContent = true;
            }
            else
            {
                abstractFigureItem.Items.First(t => t.Name == "FigureCaption").Items[0].TextAlignmentCenterForShortContent = false;
            }

            var imageTableHandler = new BizFigureTableHandler(abstractGraphicInfo, abstractFigureItem, styleName, document, parameters.ParagraphStyles);
            var imageTable = imageTableHandler.Handle();
            document.Add(imageTable);
            abstractGraphicDrawn = true;
        }

        private void PrepareHandler()
        {
            headerEventHandler = new ArticlePageStartEventHandler(document, layoutItems, articleInfo, this);
            pdfDocument.AddEventHandler(PdfDocumentEvent.START_PAGE, headerEventHandler);

            pageMarginEventHandler = new ArticlePageMarginEventHandler(document, parameters.PageBottomMargins);
            pdfDocument.AddEventHandler(PdfDocumentEvent.START_PAGE, pageMarginEventHandler);

            articlePageEndEventHanlder = new ArticlePageEndEventHandler(document, this);
            pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, articlePageEndEventHanlder);


            if (!string.IsNullOrEmpty(parameters.WatermarkContent))
            {
                var watermarkItem = layoutItems.Items.FirstOrDefault(t => t.Name == "Watermark");
                waterMarkEventHandler = new WaterMarkEventHandler(document, parameters.WatermarkContent, watermarkItem);
                waterMarkEventHandler.SetEnabled(true);
                pdfDocument.AddEventHandler(PdfDocumentEvent.START_PAGE, waterMarkEventHandler);
            }
        }

        private void InitializeDocumentInfo()
        {
            var documentInfo = pdfDocument.GetDocumentInfo();

            documentInfo.SetTitle(NUglify.Uglify.HtmlToText("<div>" + articleInfo.ArticleTitleText + "</div>").Code);
            documentInfo.SetAuthor(articleInfo.AuthorsText);
            documentInfo.SetCreator(parameters.Journal.Publisher);
            documentInfo.SetProducer(parameters.Journal.Publisher);

            
        }

        #region entity generator
        private void InitArticleInfo()
        {
            articleInfo = new ArticleInfo();
            articleInfo.Doi = article.Front.ArticleMeta.ArticleIdentifiers.First(t => t.PublicationIdentifierType == "doi").InnerXml;
            referenceIdPrefix = articleInfo.Doi.Replace("/", "_");

            articleInfo.DoiUrl = "https://doi.org/" + articleInfo.Doi;
            articleInfo.JournalShortName = article.Front.JournalMeta.JournalIds.First(t => t.JournalIdentifierType == "nlm-ta").InnerXml;
            articleInfo.JournalName = article.Front.JournalMeta.TitleGroups[0].Titles[0].InnerXml;

            var formattedJournalName = articleInfo.JournalName.Replace("&amp;", "&");
            

            if (article.Front.JournalMeta.ISSNs != null && article.Front.JournalMeta.ISSNs.Count > 0)
            {
                articleInfo.ISSN = article.Front.JournalMeta.ISSNs[0].InnerXml;
            }
            articleInfo.Subject = article.Front.ArticleMeta.ArticleCategories.SubjectGroups[0].Subjects[0].InnerXml;
            articleInfo.ArticleTitle = InitParagraph(article.Front.ArticleMeta.TitleGroup.ArticleTitle.InnerXml.Trim(), referenceIdPrefix);
            articleInfo.ArticleTitleText = article.Front.ArticleMeta.TitleGroup.ArticleTitle.InnerXml.Trim();
            articleInfo.Year = int.Parse(article.Front.ArticleMeta.PublicationDates[0].Year.InnerXml);
            articleInfo.Volume = article.Front.ArticleMeta.Volumes[0].InnerXml;

            
            if (article.Front.ArticleMeta.ELocationId != null)
            {
                articleInfo.ElocationId = article.Front.ArticleMeta.ELocationId.InnerXml;
                articleInfo.ContinuousPublish = true;
                articleInfo.StartPage = 1;
            }
            else if (article.Front.ArticleMeta.FirstPage != null && article.Front.ArticleMeta.LastPage != null)
            {
                articleInfo.StartPage = int.Parse(article.Front.ArticleMeta.FirstPage.InnerXml);
                var firstPage = article.Front.ArticleMeta.FirstPage.InnerXml;
                var lastPage = article.Front.ArticleMeta.LastPage.InnerXml;

                articleInfo.ElocationId = GetElocationId(firstPage, lastPage);
            }
            

            articleInfo.RunningTitleInFooter = string.Format("{0}. {1};{2}:{3} | {4}",
                articleInfo.JournalShortName,
                articleInfo.Year,
                articleInfo.Volume,
                articleInfo.ElocationId, 
                "<WebSite href='" + articleInfo.DoiUrl + "'>" + articleInfo.DoiUrl + "</WebSite>");

            var contributors = article.Front.ArticleMeta.ContributorGroup.Contributors.Where(t => t.ContributionType == "author").ToList();

            var contributorDictionary = new Dictionary<int, Contributor>();
            var citationBuilder = new StringBuilder();

            var authorsBuilder = new StringBuilder();
            var authorsTextBuilder = new StringBuilder();

            for (int x = 0; x < contributors.Count; x++)
            {
                var c = contributors[x];
                contributorDictionary.Add(x + 1, c);
                var authorName = string.Empty;
                if (c.PersonName != null)
                {
                    authorName = GetNameStr(c.PersonName);
                    authorsTextBuilder.Append(authorName);

                }
                else if (c.CollaborativeAuthor != null)
                {
                    authorName = c.CollaborativeAuthor.InnerXml;
                    authorsTextBuilder.Append(authorName);
                                        
                }
                if (!string.IsNullOrEmpty(authorName))
                {
                    var aArray = authorName.Split(new char[] { ' ' });

                    for (var i = 0; i < aArray.Length - 1; i++)
                    {
                        authorsBuilder.Append(aArray[i]);
                        authorsBuilder.Append(" ");
                    }
                    authorsBuilder.Append("<InlineParagraph>");
                    authorsBuilder.Append(aArray[aArray.Length - 1]);

                }

                if (c.Xrefs != null && c.Xrefs.Count > 0)
                {
                    var affXrefs = c.Xrefs.ToList();
                    if (affXrefs != null && affXrefs.Count > 0)
                    {
                        authorsBuilder.Append("<sup>");
                        for (int i = 0; i < affXrefs.Count; i++)
                        {
                            var affFlag = affXrefs[i].InnerXml.Trim().Replace("<sup>", "").Replace("</sup>", "");
                            if (!string.IsNullOrEmpty(affFlag))
                            {
                                if (affFlag != "#" && affFlag != "*" && affFlag != "†" && i > 0)
                                {
                                    authorsBuilder.Append(",");
                                }
                                if (affFlag == "*")
                                {
                                    affFlag = "<CorresFlag>*</CorresFlag>";
                                }
                                authorsBuilder.Append(affFlag);
                            }
                        }
                        authorsBuilder.Append("</sup>");
                    }
                }
                if (c.ContributorIds != null && c.ContributorIds.Count > 0)
                {
                    var orcidId = c.ContributorIds.FirstOrDefault(t => t.ContributorIdentifierType == "orcid");
                    if (orcidId != null && !string.IsNullOrEmpty(orcidId.InnerXml))
                    {
                        authorsBuilder.AppendFormat("<ORCID Href=\"{0}\">{1}</ORCID>", orcidId.InnerXml, this.parameters.ORCIDLogoPath);
                    }
                }
                if (x != contributors.Count - 1)
                {
                    authorsBuilder.Append(",</InlineParagraph> ");
                    authorsTextBuilder.Append(", ");
                }
                else
                {
                    if (!string.IsNullOrEmpty(authorName))
                    {
                        authorsBuilder.Append("</InlineParagraph>");
                    }
                }
            }
            articleInfo.Authors = InitParagraph(authorsBuilder.ToString(), referenceIdPrefix);
            articleInfo.Authors = articleInfo.Authors.Replace("</InlineParagraph> <InlineParagraph>", "</InlineParagraph><Space/><InlineParagraph>");
            articleInfo.AuthorsText = authorsTextBuilder.ToString();

            var currentContributorCount = 0;
            var restart = false;
            foreach (var kvp in contributorDictionary)
            {

                if (kvp.Value.PersonName != null)
                {
                    if (!restart)
                    {
                        currentContributorCount++;
                    }
                    if (currentContributorCount <= 6)
                    {
                        if (!restart)
                        {
                            citationBuilder.Append(InitParagraph(GetCitationName(kvp.Value.PersonName), referenceIdPrefix));
                            if (contributorDictionary.Count > kvp.Key)
                            {
                                if (contributorDictionary[kvp.Key + 1].CollaborativeAuthor != null)
                                {
                                    citationBuilder.Append("; ");
                                }
                                else
                                {
                                    citationBuilder.Append(", ");
                                }
                            }
                            else
                            {
                                if (citationBuilder[citationBuilder.Length - 1] != '.')
                                {
                                    citationBuilder.Append(".");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!restart)
                        {
                            citationBuilder.Append(" et al.");
                            if (contributorDictionary.Any(t => t.Key > kvp.Key && t.Value.CollaborativeAuthor != null))
                            {
                                citationBuilder.Append("; ");
                            }
                            else
                            {
                                break;
                            }
                            restart = true;
                        }

                    }
                }
                else if (kvp.Value.CollaborativeAuthor != null)
                {
                    citationBuilder.Append(InitParagraph(kvp.Value.CollaborativeAuthor.InnerXml, referenceIdPrefix));
                    
                    if (contributorDictionary.Count > kvp.Key)
                    {
                        citationBuilder.Append("; ");
                        currentContributorCount = 0;
                        restart = false;
                    }
                    else
                    {
                        if (citationBuilder[citationBuilder.Length - 1] != '.')
                        {
                            citationBuilder.Append(".");
                        }
                    }
                }
                
            }

            citationBuilder.Append(" ");
            citationBuilder.Append(InitParagraph(articleInfo.ArticleTitle.Trim(),referenceIdPrefix));
            if (!articleInfo.ArticleTitle.EndsWith("?")
                && !articleInfo.ArticleTitle.EndsWith(".")
                && !articleInfo.ArticleTitle.EndsWith("!"))
            {
                citationBuilder.Append(".");
            }
            citationBuilder.Append(" ");

            citationBuilder.Append(articleInfo.JournalShortName);
            citationBuilder.Append(". ");
            citationBuilder.Append(articleInfo.Year);
            citationBuilder.Append(";");
            citationBuilder.AppendFormat("{0}:{1}.", articleInfo.Volume, articleInfo.ElocationId);
            citationBuilder.Append(" ");
            citationBuilder.Append("<WebSite href='" + articleInfo.DoiUrl + "'>" + articleInfo.DoiUrl + "</WebSite>");
            articleInfo.Citation = "<Bold>Cite this article: </Bold>" + citationBuilder.ToString();

            if (article.Front.ArticleMeta.Affiliations != null && article.Front.ArticleMeta.Affiliations.Count > 0)
            {
                articleInfo.AuthorAffliations = new List<string>();
                foreach (var aff in article.Front.ArticleMeta.Affiliations)
                {
                    articleInfo.AuthorAffliations.Add(InitParagraph(aff.InnerXml, referenceIdPrefix, false, true));
                }
            }

            if (article.Front.ArticleMeta.AuthorNotes != null)
            {
                if (article.Front.ArticleMeta.AuthorNotes.Correspondences != null && article.Front.ArticleMeta.AuthorNotes.Correspondences.Count > 0)
                {
                    var corresBuilder = new StringBuilder();
                    foreach (var corres in article.Front.ArticleMeta.AuthorNotes.Correspondences)
                    {
                        var corresContent = InitParagraph(corres.InnerXml, referenceIdPrefix, false, true);
                        corresBuilder.Append(corresContent);
                        corresBuilder.Append("; ");
                    }


                    articleInfo.CorrespondenceTo = corresBuilder.ToString().Trim().TrimEnd(new char[] { ';' });
                }
                articleInfo.AuthorNotes = new List<string>();
                if (article.Front.ArticleMeta.AuthorNotes.Footnotes != null && article.Front.ArticleMeta.AuthorNotes.Footnotes.Count > 0)
                {
                    foreach (var fn in article.Front.ArticleMeta.AuthorNotes.Footnotes)
                    {
                        if (fn.Paragraphs != null && fn.Paragraphs.Count > 0)
                        {
                            var fnXml = fn.Paragraphs[0].InnerXml;
                            if (fnXml.Contains("Academic Editor"))
                            {
                                articleInfo.EditorContent = InitParagraph(fnXml, referenceIdPrefix);
                            }
                            else
                            {
                                if (fn.FootnoteType == "equal")
                                {
                                    articleInfo.AuthorNotes.Add(InitParagraph("<sup>" + fn.Label.InnerXml + "</sup>" + fn.Paragraphs[0].InnerXml, referenceIdPrefix));

                                }
                                else
                                {
                                    articleInfo.AuthorNotes.Add(InitParagraph(fnXml, referenceIdPrefix));
                                }
                            }
                        }
                    }
                }
            }

            if (parameters.ArticleExInfo != null && !string.IsNullOrEmpty(parameters.ArticleExInfo.Correspondence))
            {
                var corres = parameters.ArticleExInfo.Correspondence;
                var emails = Helpers.Regex.GetEmailsFromText(corres);
                foreach (var email in emails)
                {
                    corres = corres.Replace(email, "<email>" + email + "</email>");
                }
                corres = InitParagraph("<bold>*Correspondence:</bold> " + corres, referenceIdPrefix, false, true);
                articleInfo.CorrespondenceTo = corres;
            }

            var academicEditors = article.Front.ArticleMeta.ContributorGroup.Contributors.Where(t => t.ContributionType == "editor").ToList();

            if (academicEditors != null && academicEditors.Count > 0)
            {
                var academicEditorText = new StringBuilder();
                academicEditorText.AppendFormat("<Bold>{0}: </Bold>", academicEditors[0].Role.InnerXml);
                foreach (var academicEditor in academicEditors)
                {
                    academicEditorText.AppendFormat("{0}", GetNameStr(academicEditor.PersonName));
                    if (academicEditor.Affiliations != null && academicEditor.Affiliations.Count > 0)
                    {
                        academicEditorText.AppendFormat(", {0}", academicEditor.Affiliations[0].InnerXml);
                    }
                    academicEditorText.Append("; ");
                }
                articleInfo.EditorContent = academicEditorText.ToString().Trim().TrimEnd(new char []{ ';' });
            }

            var publishedDate = GetPubDateStr(article.Front.ArticleMeta.PublicationDates.First(t => t.PublicationType == "epub"));
            if (parameters.ArticleExInfo != null && parameters.ArticleExInfo.PublishDate.HasValue)
            {
                var inf = new CultureInfo("en-US", false);
                publishedDate = parameters.ArticleExInfo.PublishDate.Value.ToString("MMMMMMMMMMMMM d, yyyy", inf);
            }
            if (article.Front.ArticleMeta.History != null)
            {
                
                var publishedDateTitle = "Published:";
                if (article.Front.ArticleMeta.History.Dates.Any(t => t.DateType == "online-first"))
                {
                    publishedDate = GetDateStr(article.Front.ArticleMeta.History.Dates.First(t => t.DateType == "online-first"));
                    publishedDateTitle = "Online First:";
                }

                articleInfo.DateContent = string.Format("<Bold>Received:</Bold> {0}  <Bold>Accepted:</Bold> {1}  <Bold>{2}</Bold> {3}",
                    GetDateStr(article.Front.ArticleMeta.History.Dates.First(t => t.DateType == "received")),
                    GetDateStr(article.Front.ArticleMeta.History.Dates.First(t => t.DateType == "accepted")),
                    publishedDateTitle,
                    publishedDate);
            }
            else
            {
                articleInfo.DateContent = string.Format("<Bold>Published:</Bold> {0}", publishedDate);
            }


            if (article.Front.ArticleMeta.Permissions != null
                    && article.Front.ArticleMeta.Permissions.License != null
                    && article.Front.ArticleMeta.Permissions.License.LicenseParagraphs != null
                    && article.Front.ArticleMeta.Permissions.License.LicenseParagraphs.Count > 0)
            {
                var licenseParaText = article.Front.ArticleMeta.Permissions.License.LicenseParagraphs[0].InnerXml;
                var setInlineText = "License (<ext-link ext-link-type=\"uri\" xlink:href=\"https://creativecommons.org/licenses/by/4.0/\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">https://creativecommons.org/licenses/by/4.0/</ext-link>),";
                licenseParaText = licenseParaText.Replace(setInlineText, "<InlineParagraph>" + setInlineText + "</InlineParagraph>");
                setInlineText = "License (<uri xlink:href=\"https://creativecommons.org/licenses/by/4.0/\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">https://creativecommons.org/licenses/by/4.0/</uri>),";
                licenseParaText = licenseParaText.Replace(setInlineText, "<InlineParagraph>" + setInlineText + "</InlineParagraph>");
                licenseParaText = licenseParaText.Replace("and reproduction", "<InlineParagraph>and reproduction</InlineParagraph>");
                licenseParaText = licenseParaText.Replace("original author(s)", "<InlineParagraph>original author(s)</InlineParagraph>");
                articleInfo.License = InitParagraph("<Bold>" + article.Front.ArticleMeta.Permissions.CopyrightStatement.InnerXml + "</Bold> " + licenseParaText, referenceIdPrefix);
            }
            if (article.Front.ArticleMeta.Permissions.License.LicenseParagraphs.Count == 2)
            {
                articleInfo.PublisherNote = InitParagraph(article.Front.ArticleMeta.Permissions.License.LicenseParagraphs[1].InnerXml, referenceIdPrefix);
            }
            else
            {
                articleInfo.PublisherNote = "<Bold>Publisher’s note.</Bold> Open Exploration maintains a neutral stance on jurisdictional claims in published institutional affiliations and maps. All opinions expressed in this article are the personal views of the author(s) and do not represent the stance of the editorial team or the publisher.";
            }
            
            if (article.Front.ArticleMeta.Abstracts != null && article.Front.ArticleMeta.Abstracts.Count > 0)
            {
                articleInfo.AbstractContents = new Dictionary<string, string>();
                var abstractElement = article.Front.ArticleMeta.Abstracts[0];
                if (abstractElement.Sections != null && abstractElement.Sections.Count > 0)
                {
                    foreach (var sec in abstractElement.Sections)
                    {
                        var absSecTitle = sec.Title.InnerXml;
                        var firstParagraph = sec.Elements[0] as JATS.Entities.Paragraph;
                        var absSecParaInnerXml = firstParagraph.InnerXml;

                        var fpId = firstParagraph.Id;
                        if (string.IsNullOrEmpty(fpId))
                        {
                            fpId = System.Guid.NewGuid().ToString();
                        }
                        var abstractContent = string.Format("<ContentTitle>{0} </ContentTitle>{1}", absSecTitle, absSecParaInnerXml);
                        articleInfo.AbstractContents.Add(fpId, InitParagraph(abstractContent, referenceIdPrefix, false, true));

                        if (sec.Elements.Count > 1)
                        {
                            for (int i = 1; i < sec.Elements.Count; i++)
                            {
                                var paraElement = sec.Elements[i];
                                if (paraElement is JATS.Entities.Paragraph)
                                {
                                    var pId = ((JATS.Entities.Paragraph)paraElement).Id;
                                    if (string.IsNullOrEmpty(pId))
                                    {
                                        pId = System.Guid.NewGuid().ToString();
                                    }
                                    articleInfo.AbstractContents.Add(pId, InitParagraph((paraElement as JATS.Entities.Paragraph).InnerXml, referenceIdPrefix, false, true));
                                }
                            }
                        }
                    }
                    foreach (var p in abstractElement.Paragraphs)
                    {
                        if (p.InnerXml.Contains("<fig"))
                        {
                            var jatsAnalyser = new JATSAnalyzer();
                            var abstractFigure = jatsAnalyser.GetEntity<Figure>(p.InnerXml);
                            abstractGraphicInfo = GetArticleFigure(abstractFigure);
                        }
                    }
                }
                else
                {
                    foreach (var p in abstractElement.Paragraphs)
                    {
                        var abstractContent = p.InnerXml;
                        var pId = p.Id;
                        if (string.IsNullOrEmpty(pId))
                        {
                            pId = System.Guid.NewGuid().ToString();
                        }
                        if (abstractContent.Contains("<fig"))
                        {
                            var jatsAnalyser = new JATSAnalyzer();
                            var abstractFigure = jatsAnalyser.GetEntity<Figure>(abstractContent);
                            abstractGraphicInfo = GetArticleFigure(abstractFigure);
                        }
                        else
                        {
                            
                            articleInfo.AbstractContents.Add(pId, InitParagraph(abstractContent, referenceIdPrefix, false, true));
                        }
                    }
                }

                var graphicAbstract = article.Front.ArticleMeta.Abstracts.FirstOrDefault(t => t.AbstractType == "graphical");
                if (graphicAbstract != null && graphicAbstract.Paragraphs != null && graphicAbstract.Paragraphs.Count > 0)
                {
                    var jatsAnalyser = new JATSAnalyzer();
                    var abstractFigure = jatsAnalyser.GetEntity<Figure>(graphicAbstract.Paragraphs[0].InnerXml);
                    abstractGraphicInfo = GetArticleFigure(abstractFigure);
                }

                if (abstractGraphicInfo != null)
                {
                    if (tableStyles != null && tableStyles.ContainsKey(abstractGraphicInfo.Id))
                    {
                        var styleInfo = tableStyles[abstractGraphicInfo.Id];
                        if (styleInfo.ImageWidth.HasValue)
                        {
                            abstractGraphicInfo.Width = styleInfo.ImageWidth.Value;
                        }
                    }
                }
            }

            if (article.Front.ArticleMeta.keywordGroups != null
                && article.Front.ArticleMeta.keywordGroups.Count > 0
                && article.Front.ArticleMeta.keywordGroups[0].Keywords != null
                && article.Front.ArticleMeta.keywordGroups[0].Keywords.Count > 0
                && !string.IsNullOrWhiteSpace(article.Front.ArticleMeta.keywordGroups[0].Keywords[0].InnerXml))
            {
                var keywordsBuilder = new StringBuilder();
                foreach (var kw in article.Front.ArticleMeta.keywordGroups[0].Keywords)
                {
                    keywordsBuilder.Append(InitParagraph(kw.InnerXml.Trim(), referenceIdPrefix));
                    keywordsBuilder.Append(", ");
                }
                articleInfo.Keywords = keywordsBuilder.ToString().Trim().TrimEnd(new char[] { ',' });
            }

            articleInfo.ContentItems = new List<SectionInfo>();

            if (article.Front.ArticleMeta.RelatedArticles != null && article.Front.ArticleMeta.RelatedArticles.Count > 0)
            {
                var sectionInfo = new SectionInfo();
                sectionInfo.Items = new List<BodyItemBase>();
                articleInfo.ContentItems.Add(sectionInfo);

                sectionInfo.Items.Add(GetArticleParagraph(new JATS.Entities.Paragraph() { InnerXml = article.Front.ArticleMeta.RelatedArticles[0].InnerXml }, referenceIdPrefix));

            }
            if (article.Body != null && article.Body.Elements != null)
            {
                if (article.Body.Elements.Any(t => t is Section))
                {
                    foreach (var element in article.Body.Elements)
                    {
                        if (element is Section)
                        {
                            int level = 1;
                            articleInfo.ContentItems.Add(GetArticleSection(element as Section, level));
                        }
                        else if (element is JATS.Entities.Paragraph)
                        {
                            var sectionInfo = new SectionInfo();
                            sectionInfo.Items = new List<BodyItemBase>();
                            articleInfo.ContentItems.Add(sectionInfo);

                            sectionInfo.Items.Add(GetArticleParagraph(element as JATS.Entities.Paragraph, referenceIdPrefix));
                        }
                        else if (element is TableWrapper)
                        {
                            var sectionInfo = new SectionInfo();
                            sectionInfo.Items = new List<BodyItemBase>();
                            articleInfo.ContentItems.Add(sectionInfo);

                            sectionInfo.Items.Add(GetArticleTable(element as TableWrapper));
                        }
                        else if (element is JATS.Entities.Figure)
                        {
                            var sectionInfo = new SectionInfo();
                            sectionInfo.Items = new List<BodyItemBase>();
                            articleInfo.ContentItems.Add(sectionInfo);

                            sectionInfo.Items.Add(GetArticleFigure(element as JATS.Entities.Figure));
                        }
                        
                    }
                }
                else
                {
                    var sectionInfo = new SectionInfo();
                    sectionInfo.Items = new List<BodyItemBase>();
                    articleInfo.ContentItems.Add(sectionInfo);

                    foreach (var element in article.Body.Elements)
                    {
                        if (element is JATS.Entities.Paragraph)
                        {
                            if ((element as JATS.Entities.Paragraph).InnerXml.Trim().StartsWith("<list"))
                            {
                                var jatsAnalyser = new JATSAnalyzer();
                                var listWrapper = jatsAnalyser.GetEntity<ListWrapper>((element as JATS.Entities.Paragraph).InnerXml);
                                sectionInfo.Items.Add(GetArticleJatsList(listWrapper, referenceIdPrefix));
                            }
                            else
                            {
                                sectionInfo.Items.Add(GetArticleParagraph(element as JATS.Entities.Paragraph, referenceIdPrefix));
                            }
                            
                        }
                        else if (element is TableWrapper)
                        {
                            sectionInfo.Items.Add(GetArticleTable(element as TableWrapper));
                        }
                        else if (element is Figure)
                        {
                            sectionInfo.Items.Add(GetArticleFigure(element as Figure));
                        }
                    }
                }
            }

            if (article.Back != null)
            {   
                articleInfo.BackItems = new List<SectionInfo>();
                if (article.Back.Elements != null)
                {
                    var glossary = article.Back.Elements.FirstOrDefault(t => t is GlossaryElements) as GlossaryElements;
                    if (glossary != null)
                    {
                        var glossarySection = new SectionInfo()
                        {
                            Title = glossary.Title.InnerXml,

                            Level = 1,
                            Items = new List<BodyItemBase>()
                        };
                        foreach (var defItem in glossary.DefinitionLists[0].DefItems)
                        {
                            var defXml = defItem.Term.InnerXml.Trim() + ": " + defItem.Defs[0].Paragraphs[0].InnerXml;
                            var paraInfo = new ParagraphInfo();
                            paraInfo.Text = InitParagraph(defXml, referenceIdPrefix);
                            glossarySection.Items.Add(paraInfo);
                        }
                        articleInfo.BackItems.Add(glossarySection);
                    }
                }
                
                if (article.Back.FootnoteGroup != null 
                    && article.Back.FootnoteGroup.Footnotes != null
                    && article.Back.FootnoteGroup.Footnotes.Count > 0)
                {
                    var footnoteSection = new SectionInfo()
                    {
                        Title = "Footnote",
                        Level = 1,
                        Items = new List<BodyItemBase>()
                    };

                    foreach (var fn in article.Back.FootnoteGroup.Footnotes)
                    {
                        if (fn.Paragraphs != null && fn.Paragraphs.Count > 0)
                        {
                            fn.Paragraphs[0].Id = referenceIdPrefix + fn.Id;
                            var paraInfo = GetArticleParagraph(fn.Paragraphs[0], referenceIdPrefix);
                            paraInfo.SetDestination = true;
                            footnoteSection.Items.Add(paraInfo);
                        }
                    }

                    articleInfo.BackItems.Add(footnoteSection);
                }
                if (article.Back.Elements != null && article.Back.Elements.Count > 0)
                {
                    
                    foreach (var sec in article.Back.Elements)
                    {
                        if (sec is Section)
                        {
                            int level = 1;

                            articleInfo.BackItems.Add(GetArticleSection(sec as Section, level));
                        }
                        
                    }
                }
                if (article.Back.ReferenceList != null && article.Back.ReferenceList.References != null)
                {
                    articleInfo.Refs = new List<Reference>();
                    foreach (var r in article.Back.ReferenceList.References)
                    {
#if DEBUG
                        if (r.Id == "B41")
                        {

                        }
#endif
                        var mRef = new Reference();
                        mRef.Label = r.Label.InnerXml;
                        mRef.Id = referenceIdPrefix + r.Id;
                        mRef.OriginalId = r.Id;
                        articleInfo.Refs.Add(mRef);

                        string refPubMed = string.Empty;
                        string refDoi = string.Empty;
                        string refPMCID = string.Empty;

                        StringBuilder refBuilder = new StringBuilder();
                        var authorBuilder = new StringBuilder();
                        var editorBuilder = new StringBuilder();
                        var assigneeBuiler = new StringBuilder();

                        var authorStr = string.Empty;
                        var editorStr = string.Empty;

                        PersonGroup authorGroup = null;
                        PersonGroup editorGroup = null;


                        bool hasCollab = false;
                        if (r.ElementCitation != null && r.ElementCitation.NamingElements != null)
                        {
                            foreach (var namingElement in r.ElementCitation.NamingElements)
                            {
                                if (namingElement is PersonGroup)
                                {
                                    var personGroup = namingElement as PersonGroup;
                                    if (personGroup.PersonGroupType == "author" || personGroup.PersonGroupType == "inventor")
                                    {
                                        authorGroup = personGroup;
                                        var nameIndex = 0;
                                        foreach (var name in personGroup.Names)
                                        {
                                            if (name is PersonName)
                                            {
                                                var personName = name as PersonName;
                                                var nameStr = GetNameStr(personName, true);
                                                authorBuilder.Append(nameStr);
                                                authorBuilder.Append(", ");
                                            }
                                            if (name is EtAl)
                                            {
                                                authorBuilder.Append("et al");
                                                if (personGroup.PersonGroupType == "inventor")
                                                {
                                                    authorBuilder.Append("., ");
                                                }
                                                else
                                                {

                                                    if (nameIndex < personGroup.Names.Count - 1 && (personGroup.Names[nameIndex - 1] is CollaborativeAuthor))
                                                    {
                                                        authorBuilder.Append(".; ");
                                                    }
                                                    else
                                                    {
                                                        authorBuilder.Append(". ");
                                                    }
                                                }
                                            }
                                            else if (name is CollaborativeAuthor)
                                            {
                                                var collabAuthor = name as CollaborativeAuthor;
                                                if (!hasCollab)
                                                {
                                                    if (authorBuilder.Length > 0)
                                                    {
                                                        authorBuilder = authorBuilder.Remove(authorBuilder.Length - 2, 2);
                                                        if (authorBuilder.ToString().EndsWith("et al"))
                                                        {
                                                            authorBuilder.Append(".");
                                                        }
                                                        authorBuilder.Append("; ");
                                                    }
                                                }
                                                else
                                                {
                                                    hasCollab = true;
                                                }
                                                authorBuilder.Append(InitParagraph(collabAuthor.InnerXml, referenceIdPrefix));
                                                if (nameIndex < personGroup.Names.Count - 1 && (personGroup.Names[nameIndex + 1] is EtAl))
                                                {
                                                    authorBuilder.Append(", ");
                                                }
                                                else
                                                {
                                                    authorBuilder.Append("; ");
                                                }
                                            }
                                            nameIndex++;
                                        }
                                        if (personGroup.PersonGroupType == "inventor")
                                        {
                                            if (authorGroup.Names.Count > 1)
                                            {
                                                authorBuilder.Append("inventors.");
                                            }
                                            else
                                            {
                                                authorBuilder.Append("inventor.");
                                            }
                                        }
                                    }
                                    else if (personGroup.PersonGroupType == "editor")
                                    {
                                        editorGroup = personGroup;
                                        var editorStrBuilder = new StringBuilder();
                                        foreach (var name in editorGroup.Names)
                                        {
                                            if (name is PersonName)
                                            {
                                                var personName = name as PersonName;
                                                var nameStr = GetNameStr(personName, true);
                                                editorStrBuilder.Append(nameStr);
                                                editorStrBuilder.Append(", ");
                                            }
                                            else if (name is CollaborativeAuthor)
                                            {
                                                editorStrBuilder.Append((name as CollaborativeAuthor).InnerXml);
                                                editorStrBuilder.Append("; ");
                                            }
                                            else if (name is EtAl)
                                            {
                                                editorStrBuilder.Append("et al.");
                                                editorStrBuilder.Append(", ");
                                            }
                                        }
                                        var editorStrV = editorStrBuilder.ToString().Trim();
                                        editorStrV = editorStrV.Substring(0, editorStrV.Length - 1);
                                        editorBuilder.Append(editorStrV);
                                        if (editorGroup.Names.Count > 1)
                                        {
                                            editorBuilder.Append(", editors.");
                                        }
                                        else
                                        {
                                            editorBuilder.Append(", editor.");
                                        }
                                    }
                                }
                                else if (namingElement is CollaborativeAuthor)
                                {
                                    var collab = namingElement as CollaborativeAuthor;
                                    if (collab.CollaborationType == "assignee")
                                    {
                                        assigneeBuiler.Append((namingElement as CollaborativeAuthor).InnerXml);
                                        assigneeBuiler.Append(", ");
                                    }
                                }
                            }
                        }

                        authorStr = authorBuilder.ToString().Trim();
                        if (assigneeBuiler.Length > 0)
                        {
                            authorStr = authorStr.TrimEnd('.');
                            assigneeBuiler.Append("assignee.");
                            authorStr += "; ";
                            authorStr += assigneeBuiler.ToString();
                        }
                        else
                        {

                            if (authorStr.EndsWith(","))
                            {
                                authorStr = authorStr.TrimEnd(new char[] { ',' }) + ".";
                            }
                            if (authorStr.EndsWith(";"))
                            {
                                authorStr = authorStr.TrimEnd(new char[] { ';' }) + ".";
                            }
                        }
                        bool hasAuthor = false;
                        if (!string.IsNullOrEmpty(authorStr))
                        {
                            refBuilder.Append(authorStr + " ");
                            hasAuthor = true;
                        }
                        else if (r.ElementCitation.PublicationType != ReferencedPublicationType.Book)
                        {
                            if (editorBuilder.Length > 0)
                            {
                                refBuilder.Append(editorBuilder);
                                refBuilder.Append(" ");
                            }
                        }
                        
                        
                        if (string.IsNullOrEmpty(r.ElementCitation.PublicationType))
                        {
                            r.ElementCitation.PublicationType = r.ElementCitation.CitationType;
                        }

                        if (r.ElementCitation.PublicationType == ReferencedPublicationType.JournalArticle)
                        {
                            if (r.ElementCitation.ArticleTitle != null && !string.IsNullOrEmpty(r.ElementCitation.ArticleTitle.InnerXml))
                            {
                                string articlePage = null;
                                if (r.ElementCitation.FirstPage != null &&
                                    !string.IsNullOrEmpty(r.ElementCitation.FirstPage.InnerXml)
                                    && r.ElementCitation.LastPage != null
                                    && !string.IsNullOrEmpty(r.ElementCitation.LastPage.InnerXml))
                                {
                                    articlePage = GetElocationId(r.ElementCitation.FirstPage.InnerXml, r.ElementCitation.LastPage.InnerXml);
                                }
                                else if (r.ElementCitation.FirstPage != null && !string.IsNullOrEmpty(r.ElementCitation.FirstPage.InnerXml))
                                {
                                    articlePage = r.ElementCitation.FirstPage.InnerXml;
                                }
                                else if (r.ElementCitation.LastPage != null && !string.IsNullOrEmpty(r.ElementCitation.LastPage.InnerXml))
                                {
                                    articlePage = r.ElementCitation.LastPage.InnerXml;
                                }
                                else if (r.ElementCitation.ELocationId != null && !string.IsNullOrEmpty(r.ElementCitation.ELocationId.InnerXml))
                                {
                                    articlePage = r.ElementCitation.ELocationId.InnerXml;
                                }
                                string articleRefTitle = r.ElementCitation.ArticleTitle.InnerXml.Trim();
                                if (!articleRefTitle.EndsWith(".") && !articleRefTitle.EndsWith("?") && !articleRefTitle.EndsWith("!"))
                                {
                                    articleRefTitle += ".";
                                }
                                //articleRefTitle = HandleHyphen(articleRefTitle);
                                refBuilder.AppendFormat("{0} {1}{2}{3}{4}{5}",
                                    InitParagraph(articleRefTitle, referenceIdPrefix),
                                    (r.ElementCitation.Source != null && !string.IsNullOrEmpty(r.ElementCitation.Source.InnerXml)) ?
                                    r.ElementCitation.Source.InnerXml.TrimEnd(new char[] { '.' }) + "." + " " : "",
                                    (r.ElementCitation.Year != null && !string.IsNullOrEmpty(r.ElementCitation.Year.InnerXml)) ?
                                    r.ElementCitation.Year.InnerXml : "",
                                    (r.ElementCitation.Volume != null && !string.IsNullOrEmpty(r.ElementCitation.Volume.InnerXml)) ?
                                    ";" + r.ElementCitation.Volume.InnerXml : "",
                                    (r.ElementCitation.Supplement == null || string.IsNullOrEmpty(r.ElementCitation.Supplement.InnerXml)) ?
                                    "" : " " + r.ElementCitation.Supplement.InnerXml + " ",
                                    string.IsNullOrEmpty(articlePage) ? "" : ":" + articlePage);
                            }
                        }
                        else if (r.ElementCitation.PublicationType == ReferencedPublicationType.Book)
                        {
                            string bookPage = null;
                            if (r.ElementCitation.FirstPage != null && !string.IsNullOrEmpty(r.ElementCitation.FirstPage.InnerXml)
                                && r.ElementCitation.LastPage != null && !string.IsNullOrEmpty(r.ElementCitation.LastPage.InnerXml))
                            {
                                bookPage = r.ElementCitation.FirstPage.InnerXml + "-" + r.ElementCitation.LastPage.InnerXml;
                            }
                            else if (r.ElementCitation.FirstPage != null && !string.IsNullOrEmpty(r.ElementCitation.FirstPage.InnerXml))
                            {
                                bookPage = r.ElementCitation.FirstPage.InnerXml;
                            }
                            else if (r.ElementCitation.LastPage != null && !string.IsNullOrEmpty(r.ElementCitation.LastPage.InnerXml))
                            {
                                bookPage = r.ElementCitation.LastPage.InnerXml;
                            }
                            string pageLinkStr = string.Empty;
                            if (!string.IsNullOrEmpty(bookPage))
                            {
                                pageLinkStr = ". pp. ";
                                if (!bookPage.Contains("-"))
                                {
                                    pageLinkStr = ". p. ";
                                }
                            }

                            

                            var bookInfo = new StringBuilder();
                            if (r.ElementCitation.ArticleTitle == null || string.IsNullOrEmpty(r.ElementCitation.ArticleTitle.InnerXml))
                            {
                                if (string.IsNullOrEmpty(authorStr))
                                {
                                    authorStr = editorBuilder.ToString();
                                }
                                else
                                {
                                    authorStr = "";
                                    editorStr = editorBuilder.ToString();
                                }
                                bookInfo.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}",
                                    string.IsNullOrEmpty(authorStr) ? "" : authorStr + " ",
                                    (r.ElementCitation.Source == null || string.IsNullOrEmpty(r.ElementCitation.Source.InnerXml)) ?
                                    "" : InitParagraph(r.ElementCitation.Source.InnerXml, referenceIdPrefix) + ". ",
                                    (r.ElementCitation.Edition == null || string.IsNullOrEmpty(r.ElementCitation.Edition.InnerXml)) ?
                                    "" : r.ElementCitation.Edition.InnerXml.Trim().TrimEnd(new char[] { '.' }) + ". ",
                                    !string.IsNullOrEmpty(editorStr) ? editorStr + " " : "",
                                    (r.ElementCitation.PublisherLocations == null || r.ElementCitation.PublisherLocations.Count <= 0) ?
                                    "" : r.ElementCitation.PublisherLocations[0].InnerXml.Trim() + ": ",

                                    (r.ElementCitation.PublisherNames == null || r.ElementCitation.PublisherNames.Count() <= 0) ?
                                    "" : r.ElementCitation.PublisherNames[0].InnerXml + "; ",
                                    (r.ElementCitation.Year != null && !string.IsNullOrEmpty(r.ElementCitation.Year.InnerXml)) ?
                                    r.ElementCitation.Year.InnerXml : "",
                                    string.IsNullOrEmpty(bookPage) ? "" : pageLinkStr + bookPage);
                            }
                            else
                            {
                                
                                editorStr = editorBuilder.ToString();
                                
                                if (!string.IsNullOrEmpty(editorStr))
                                {
                                    editorStr = "In: " + editorStr;
                                }
                                var articleTitle = r.ElementCitation.ArticleTitle.InnerXml.Trim();
                                articleTitle = InitParagraph(articleTitle, referenceIdPrefix);
                                if (!articleTitle.EndsWith("?") && !articleTitle.EndsWith("!"))
                                {
                                    articleTitle = articleTitle + ".";
                                }
                                bookInfo.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}",
                                    articleTitle + " ",
                                    string.IsNullOrEmpty(editorStr) ? "" : editorStr + " ",
                                    (r.ElementCitation.Source == null || string.IsNullOrEmpty(r.ElementCitation.Source.InnerXml)) ?
                                    "" : InitParagraph(r.ElementCitation.Source.InnerXml, referenceIdPrefix) + ". ",
                                    (r.ElementCitation.Edition == null || string.IsNullOrEmpty(r.ElementCitation.Edition.InnerXml)) ?
                                    "" : r.ElementCitation.Edition.InnerXml.Trim().TrimEnd(new char[] { '.' }) + ". ",
                                    (r.ElementCitation.PublisherLocations == null || r.ElementCitation.PublisherLocations.Count <= 0) ?
                                    "" : r.ElementCitation.PublisherLocations[0].InnerXml.Trim() + ": ",

                                    (r.ElementCitation.PublisherNames == null || r.ElementCitation.PublisherNames.Count() <= 0) ?
                                    "" : r.ElementCitation.PublisherNames[0].InnerXml + "; ",
                                    (r.ElementCitation.Year != null && !string.IsNullOrEmpty(r.ElementCitation.Year.InnerXml)) ?
                                    r.ElementCitation.Year.InnerXml : "",
                                    string.IsNullOrEmpty(bookPage) ? "" : pageLinkStr + bookPage);
                            }
                            if (bookInfo.Length > 0)
                            {
                                var bookInfoStr = bookInfo.ToString().Trim();
                                if (!bookInfoStr.EndsWith(".") 
                                    && !bookInfoStr.Trim().EndsWith(";") 
                                    && !bookInfoStr.Trim().EndsWith("?")
                                    && !bookInfoStr.Trim().EndsWith("!"))
                                {
                                    bookInfo.Append(".");
                                }
                                refBuilder.Append(bookInfoStr);
                            }
                        }
                        else if (r.ElementCitation.PublicationType == ReferencedPublicationType.ConferenceProceedings)
                        {
                            if (r.ElementCitation.Comment != null
                                && r.ElementCitation.Comment.ExLinks != null
                                && r.ElementCitation.Comment.ExLinks.Count > 0)
                            {
                                ExternalLink exLink = r.ElementCitation.Comment.ExLinks.FirstOrDefault(s => s.Type == "uri");
                                if (exLink != null)
                                {
                                    string exLinkText = r.ElementCitation.Comment.InnerXml;
                                    refBuilder.Append(InitParagraph(exLinkText.Trim(), referenceIdPrefix));
                                }
                            }
                            else if (r.ElementCitation.ArticleTitle != null && !string.IsNullOrEmpty(r.ElementCitation.ArticleTitle.InnerXml))
                            {
                                refBuilder.AppendFormat("{0}. {1}, {2};{3}",
                                    InitParagraph(r.ElementCitation.ArticleTitle.InnerXml, referenceIdPrefix),
                                    r.ElementCitation.ConferenceName.InnerXml,
                                    r.ElementCitation.ConferenceDate.InnerXml,
                                    r.ElementCitation.ConferenceLocation.InnerXml);
                            }
                            else if (r.ElementCitation.Comment != null && !string.IsNullOrEmpty(r.ElementCitation.Comment.InnerXml))
                            {
                                refBuilder.Append(InitParagraph(r.ElementCitation.Comment.InnerXml, referenceIdPrefix));
                            }
                        }
                        else if (r.ElementCitation.PublicationType == ReferencedPublicationType.Website
                            || r.ElementCitation.PublicationType == ReferencedPublicationType.Other
                            || r.ElementCitation.PublicationType == ReferencedPublicationType.Report)
                        {
                            if (r.ElementCitation.Comment != null)
                            {
                                string exLinkText = InitParagraph(r.ElementCitation.Comment.InnerXml, referenceIdPrefix);

                                refBuilder.AppendFormat("{0}{1}{2}",
                                    (r.ElementCitation.ArticleTitle != null && !string.IsNullOrEmpty(r.ElementCitation.ArticleTitle.InnerXml)) ?
                                    InitParagraph(r.ElementCitation.ArticleTitle.InnerXml + ".", referenceIdPrefix) + " " : "",
                                    (r.ElementCitation.Year != null && !string.IsNullOrEmpty(r.ElementCitation.Year.InnerXml)) ?
                                    r.ElementCitation.Year.InnerXml + ";" : "",
                                    exLinkText);
                            }

                        }
                        else if (r.ElementCitation.PublicationType == ReferencedPublicationType.Patent)
                        {
                            if (r.ElementCitation.ArticleTitle != null)
                            {
                                refBuilder.AppendFormat("{0}. ", InitParagraph(r.ElementCitation.ArticleTitle.InnerXml, referenceIdPrefix));
                            }
                            if (r.ElementCitation.Source != null)
                            {
                                refBuilder.AppendFormat("{0}. ", InitParagraph(r.ElementCitation.Source.InnerXml, referenceIdPrefix));
                            }
                            if (r.ElementCitation.Patent != null)
                            {
                                refBuilder.AppendFormat("{0}. ", r.ElementCitation.Patent.InnerXml);
                            }
                            if (r.ElementCitation.Year != null)
                            {
                                refBuilder.AppendFormat("{0} {1} {2}.", 
                                    r.ElementCitation.Year.InnerXml, 
                                    r.ElementCitation.Month.InnerXml, r.ElementCitation.Day.InnerXml);
                            }
                            
                            if (r.ElementCitation.Comment != null)
                            {
                                refBuilder.Append(" " + InitParagraph(r.ElementCitation.Comment.InnerXml, referenceIdPrefix) + ".");
                            }
                        }
                        string thisRef = refBuilder.ToString().Trim();
                        if (!thisRef.EndsWith(".") 
                            && !thisRef.EndsWith(";")
                            && !thisRef.EndsWith("?")
                            && !thisRef.EndsWith("!"))
                        {
                            thisRef += ".";
                        }
                        if (r.ElementCitation.PublicationType == ReferencedPublicationType.JournalArticle
                            || r.ElementCitation.PublicationType == ReferencedPublicationType.Book
                            || r.ElementCitation.PublicationType == ReferencedPublicationType.Thesis)
                        {
                            if (r.ElementCitation.Comment != null && !string.IsNullOrEmpty(r.ElementCitation.Comment.InnerXml))
                            {
                                thisRef += (" " + InitParagraph(r.ElementCitation.Comment.InnerXml, referenceIdPrefix));
                            }
                        }

                        if (thisRef.EndsWith("</WebSite>.") || thisRef.EndsWith(")."))
                        {
                            thisRef = thisRef.TrimEnd(new char[] { '.' });
                        }
                        else if (thisRef.EndsWith(";"))
                        {
                            thisRef = thisRef.Substring(0, thisRef.Length - 1) + ".";
                        }
                        //thisRef = thisRef.Replace("<Superscript>*</Superscript>", "*");

                        if (r.ElementCitation.PubIds != null)
                        {
                            PublicationIdentifier piDoi = r.ElementCitation.PubIds.Find(t => t.PublicationIdentifierType == Helpers.Enum.GetDescription<PublicationIdentifierType>(PublicationIdentifierType.Doi));
                            if (piDoi != null)
                            {
                                refDoi = piDoi.InnerXml;
                            }

                            PublicationIdentifier pubmedDoi = r.ElementCitation.PubIds.Find(t => t.PublicationIdentifierType == Helpers.Enum.GetDescription<PublicationIdentifierType>(PublicationIdentifierType.Pmid));
                            if (pubmedDoi != null)
                            {
                                refPubMed = pubmedDoi.InnerXml;
                            }

                            PublicationIdentifier pmcId = r.ElementCitation.PubIds.Find(t => t.PublicationIdentifierType == Helpers.Enum.GetDescription<PublicationIdentifierType>(PublicationIdentifierType.Pmcid));
                            if (pmcId != null)
                            {
                                refPMCID = pmcId.InnerXml;
                            }

                            StringBuilder indexBuilder = new StringBuilder();
                            if (!string.IsNullOrEmpty(refPubMed)
                                || !string.IsNullOrEmpty(refDoi)
                                || !string.IsNullOrEmpty(refPMCID))
                            {
                                if (!string.IsNullOrEmpty(refDoi))
                                {
                                    indexBuilder.AppendFormat("<Space /><InlineParagraph>[<WebSite href='{0}'>{1}</WebSite>]</InlineParagraph>", "https://dx.doi.org/" + refDoi, "DOI");
                                }

                                if (!string.IsNullOrEmpty(refPubMed))
                                {
                                    indexBuilder.AppendFormat("<Space /><InlineParagraph>[<WebSite href='{0}'>{1}</WebSite>]</InlineParagraph>",
                                        "http://www.ncbi.nlm.nih.gov/pubmed/" + refPubMed, "PubMed");
                                }
                                if (!string.IsNullOrEmpty(refPMCID))
                                {
                                    indexBuilder.AppendFormat("<Space /><InlineParagraph>[<WebSite href='{0}'>{1}</WebSite>]</InlineParagraph>",
                                        "https://www.ncbi.nlm.nih.gov/pmc/articles/" + refPMCID, "PMC");
                                }

                                thisRef += indexBuilder.ToString();


                            }
                        }
                        mRef.Text = thisRef;
                    }
                }

            }

            
        }

        private static string ReplaceHtmlEscape(string text)
        {
            foreach (KeyValuePair<string, string> kvp in htmlEscapeDecimal)
            {
                if (text.Contains(kvp.Key))
                {
                    text = text.Replace(kvp.Key, kvp.Value);
                }
            }
            return text;
        }

        private static string PrepareArticleXml(string xml)
        {
            xml = xml.Replace("&nbsp;", "&#160;");
            xml = ReplaceHtmlEscape(xml);
            return xml;
        }

        
        private static string GetNameStr(PersonName personName, bool isReference = false)
        {
            var nameStr = string.Empty;
            var nameBuilder = new StringBuilder();
            if (!isReference)
            {
                if (personName.GivenNames != null)
                {
                    nameBuilder.Append(personName.GivenNames.InnerXml);
                }
                if (personName.Surname != null && !string.IsNullOrEmpty(personName.Surname.InnerXml))
                {
                    if (nameBuilder.Length > 0)
                    {
                        nameBuilder.Append(" ");
                    }
                    nameBuilder.Append(personName.Surname.InnerXml);
                }

                if (personName.Suffix != null && !string.IsNullOrEmpty(personName.Suffix.InnerXml))
                {
                    nameBuilder.Append(" " + personName.Suffix.InnerXml);
                }

                nameStr = nameBuilder.ToString().Trim();
                if (nameStr.Contains(" Jr. "))
                {
                    nameStr = nameStr.Replace("Jr. ", "") + " Jr.";
                }
            }
            else
            {
                if (personName.Surname != null)
                {
                    nameBuilder.Append(personName.Surname.InnerXml);
                }
                if (personName.GivenNames != null && !string.IsNullOrEmpty(personName.GivenNames.InnerXml))
                {
                    if (nameBuilder.Length > 0)
                    {
                        nameBuilder.Append(" ");
                    }
                    nameBuilder.Append(personName.GivenNames.InnerXml);
                }

                nameStr = nameBuilder.ToString().Trim();
            }




            return nameStr;
        }

        private static string GetCitationName(PersonName personName)
        {
            var nameBuilder = new StringBuilder();
            if (personName.Surname != null)
            {
                nameBuilder.Append(personName.Surname.InnerXml);
            }
            if (personName.GivenNames != null && !string.IsNullOrEmpty(personName.GivenNames.InnerXml))
            {
                if (nameBuilder.Length > 0)
                {
                    nameBuilder.Append(" ");
                }
                var array = personName.GivenNames.InnerXml.Split(new char[] { ' ' });
                foreach (var s in array)
                {
                    var subArray = s.Split(new char[] { '-' });
                    foreach (var ss in subArray)
                    {
                        var subArray2 = ss.Split(new char[] { '.' });
                        foreach (var sss in subArray2)
                        {
                            if (!string.IsNullOrEmpty(sss))
                            {
                                if (sss == "Jr")
                                {
                                    nameBuilder.Append(" Jr");
                                }
                                else
                                {
                                    if (char.IsUpper(sss[0]))
                                    {
                                        foreach (var c in sss)
                                        {
                                            if (char.IsUpper(c))
                                            {
                                                nameBuilder.Append(c);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        nameBuilder.Append(sss[0]);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            if (personName.Suffix != null && !string.IsNullOrEmpty(personName.Suffix.InnerXml))
            {
                nameBuilder.Append(" ");
                nameBuilder.Append(personName.Suffix.InnerXml);
            }
            return nameBuilder.ToString().Trim();
        }

        private SectionInfo GetArticleSection(Section section, int level)
        {
            var sectionInfo = new SectionInfo();
            sectionInfo.Level = level;
            sectionInfo.Items = new List<BodyItemBase>();

            sectionInfo.Title = InitParagraph(section.Title.InnerXml, referenceIdPrefix);
            sectionInfo.Id = section.Id;
            //Console.WriteLine(section.Id);
            foreach (var subItem in section.Elements)
            {
                if (subItem is Section)
                {
                    var sec = subItem as Section;
                    if (string.IsNullOrEmpty(sec.SectionType) || sec.SectionType != "supplementary-material")
                    {
                        sectionInfo.Items.Add(GetArticleSection(subItem as Section, level + 1));
                    }
                }
                else if (subItem is JATS.Entities.Paragraph)
                {

                    if ((subItem as JATS.Entities.Paragraph).InnerXml.Trim().StartsWith("<list"))
                    {
                        var jatsAnalyser = new JATSAnalyzer();
                        var listWrapper = jatsAnalyser.GetEntity<ListWrapper>((subItem as JATS.Entities.Paragraph).InnerXml);
                        sectionInfo.Items.Add(GetArticleJatsList(listWrapper, referenceIdPrefix));
                    }
                    else
                    {
                        sectionInfo.Items.Add(GetArticleParagraph(subItem as JATS.Entities.Paragraph, referenceIdPrefix));
                    }
                }
                else if (subItem is TableWrapper)
                {
                    sectionInfo.Items.Add(GetArticleTable(subItem as TableWrapper));
                }
                else if (subItem is Figure)
                {
                    sectionInfo.Items.Add(GetArticleFigure(subItem as Figure));
                }
                else if (subItem is JATS.Entities.SupplementaryMaterial)
                {
                    //sectionInfo.Items.Add(GetSupplementaryMaterialParagraph(subItem as SupplementaryMaterial));
                }
                else if (subItem is ListWrapper)
                {
                    sectionInfo.Items.Add(GetArticleJatsList(subItem as ListWrapper, referenceIdPrefix));
                }
            }

            return sectionInfo;
        }

        protected static JatsListInfo GetArticleJatsList(ListWrapper list, string referenceIdPrefix)
        {
            var jatsListInfo = new JatsListInfo()
            {
                Id = list.Id,
                ListType = list.ListType,
                Items = new List<JatsListItemInfo>()
            };
            foreach (var listItem in list.Items)
            {
                jatsListInfo.Items.Add(GetArticleJatsListItem(listItem, referenceIdPrefix));
            }
            return jatsListInfo;
        }

        protected static JatsListItemInfo GetArticleJatsListItem(ListWrapperItem listItem, string referenceIdPrefix)
        {
            var jatsListItem = new JatsListItemInfo() { Elements = new List<BodyItemBase>() };
            if (listItem.Label != null && !string.IsNullOrEmpty(listItem.Label.InnerXml))
            {
                jatsListItem.Symbol = listItem.Label.InnerXml;
            }
            foreach (var element in listItem.Elements)
            {
                if (element is JATS.Entities.Paragraph)
                {
                    if ((element as JATS.Entities.Paragraph).InnerXml.Trim().StartsWith("<list"))
                    {
                        var jatsAnalyser = new JATSAnalyzer();
                        var listWrapper = jatsAnalyser.GetEntity<ListWrapper>((element as JATS.Entities.Paragraph).InnerXml);
                        jatsListItem.Elements.Add(GetArticleJatsList(listWrapper, referenceIdPrefix));
                    }
                    else
                    {
                        jatsListItem.Elements.Add(GetArticleParagraph(element as JATS.Entities.Paragraph, referenceIdPrefix));
                    }
                }
                else if (element is JATS.Entities.ListWrapper)
                {
                    jatsListItem.Elements.Add(GetArticleJatsList(element as JATS.Entities.ListWrapper, referenceIdPrefix));
                }
            }
            return jatsListItem;
        }
        private static ParagraphInfo GetArticleParagraph(JATS.Entities.Paragraph p, string referenceIdPrefix)
        {
            var paraInfo = new ParagraphInfo() { Id = p.Id };
            paraInfo.Text = InitParagraph(p.InnerXml, referenceIdPrefix, false, true);
            return paraInfo;
        }

        private ParagraphInfo GetSupplementaryMaterialParagraph(JATS.Entities.SupplementaryMaterial supplementaryMaterial)
        {
            var paraInfo = new ParagraphInfo();
            if (supplementaryMaterial.Media != null)
            {
                paraInfo.Text = string.Format("<WebSite href='{0}'>{1}</WebSite>", supplementaryMaterial.Media.Href, supplementaryMaterial.Media.Caption.Paragraphs[0].InnerXml);
            }
            return paraInfo;
        }

        private TableInfo GetArticleTable(TableWrapper table)
        {
            var tableInfo = new TableInfo();

            tableInfo.Id = referenceIdPrefix + table.Id;

            if (parameters.TablsStyles != null && parameters.TablsStyles.ContainsKey(table.Id))
            {
                tableStyles.Add(tableInfo.Id, parameters.TablsStyles[table.Id]);
            }

            tableInfo.Label = table.Label.InnerXml;
            if (table.Caption != null && table.Caption.Paragraphs != null && table.Caption.Paragraphs.Count > 0)
            {
                //if (table.Caption.Paragraphs[0].InnerXml.StartsWith("Table"))
                //{
                //    tableInfo.Caption = InitParagraph(table.Caption.Paragraphs[0].InnerXml);
                //}
                //else
                {
                    tableInfo.Caption = "<Bold>" + tableInfo.Label + ".</Bold> " + InitParagraph(table.Caption.Paragraphs[0].InnerXml, referenceIdPrefix, false, true);
                    tableInfo.CaptionParagraphId = table.Caption.Paragraphs[0].Id;
                }
            }
            if (table.Graphics != null && table.Graphics.Count > 0)
            {
                tableInfo.Graphics = new List<GraphicInfo>();
                foreach (var g in table.Graphics)
                {
                    string gImageFileName = System.IO.Path.GetFileName(g.Href).Replace(".tif", ".jpg");
                    var files = Directory.GetFiles(parameters.FileOutputDir, "*" + gImageFileName, SearchOption.AllDirectories);
                    if (files != null && files.Count() > 0)
                    {
                        var tg = new GraphicInfo() { Id = g.Id, ImageUrl = files[0] };
                        tableInfo.Graphics.Add(tg);
                    }
                }
            }
            tableInfo.InnerXml = table.Table.InnerXml;// InitParagraph(table.Table.InnerXml, referenceIdPrefix, true);

            
            if (!string.IsNullOrEmpty(table.Table.Width))
            {
                tableInfo.Width = Helpers.Convert.ToFloat(table.Table.Width);
            }


            if (table.Footer != null)
            {
                List<Footnote> tableFootNotes = null;
                if (table.Footer.Footnotes != null && table.Footer.Footnotes.Count > 0)
                {
                    tableFootNotes = table.Footer.Footnotes;
                }
                else if (table.Footer.FootnoteGroup != null && table.Footer.FootnoteGroup.Footnotes != null)
                {
                    tableFootNotes = table.Footer.FootnoteGroup.Footnotes;
                }
                if (tableFootNotes != null && tableFootNotes.Count > 0)
                {
                    tableInfo.Footnotes = new List<FootnoteInfo>();
                    foreach (var fn in tableFootNotes)
                    {
                        var footNoteInfo = new FootnoteInfo();
                        tableInfo.Footnotes.Add(footNoteInfo);
                        footNoteInfo.Id = referenceIdPrefix + fn.Id;
                        if (fn.Label != null)
                        {
                            footNoteInfo.Label = fn.Label.InnerXml;
                        }
                        string fnPrefix = string.Empty;
                        if (!string.IsNullOrEmpty(footNoteInfo.Label))
                        {
                            fnPrefix = "<Superscript>" + footNoteInfo.Label + "</Superscript>";
                        }
                        footNoteInfo.Contents = fnPrefix + InitParagraph(fn.Paragraphs[0].InnerXml, referenceIdPrefix, false, true, false);
                        footNoteInfo.ContentParagraphId = fn.Paragraphs[0].Id;
                        //footNoteInfo.Contents = footNoteInfo.Contents.Replace("<Superscript>*</Superscript>", "*");

                    }
                }
            }


            return tableInfo;

        }

        private FigureInfo GetArticleFigure(Figure figure)
        {
            var figureInfo = new FigureInfo();
            figureInfo.Id = referenceIdPrefix + figure.Id;

            if (parameters.TablsStyles != null
                && parameters.TablsStyles.ContainsKey(figure.Id))
            {
                tableStyles.Add(figureInfo.Id, parameters.TablsStyles[figure.Id]);
            }

            if (figure.Id.StartsWith("eq"))
            {
                figureInfo.ImageType = ImageType.Equation;
            }
            else if (figure.Id.StartsWith("scheme"))
            {
                figureInfo.ImageType = ImageType.Scheme;
            }
            else
            {
                figureInfo.ImageType = ImageType.Image;
            }
            if (figure.Label != null)
            {
                figureInfo.Label = figure.Label.InnerXml;
            }

            if (figure.Caption != null)
            {
                if (figure.Caption.Paragraphs != null && figure.Caption.Paragraphs.Count > 0)
                {
                    figureInfo.Captions = new Dictionary<string, string>();
                    StringBuilder figCap = new StringBuilder();
                    figCap.Append(InitParagraph(figure.Caption.Paragraphs[0].InnerXml.Trim(), referenceIdPrefix, false, true));
                    var fpId = figure.Caption.Paragraphs[0].Id;
                    if (string.IsNullOrEmpty(fpId))
                    {
                        fpId = Guid.NewGuid().ToString();
                    }
                    figureInfo.Captions.Add(fpId, string.Format("<TextTitle>{0}.</TextTitle> {1}",
                        figureInfo.Label.TrimEnd(new char[] { '.' }),
                        figCap.ToString()));

                    if (figure.Caption.Paragraphs.Count > 1)
                    {
                        for (int i = 1; i < figure.Caption.Paragraphs.Count; i++)
                        {
                            var pId = figure.Caption.Paragraphs[i].Id;
                            if (string.IsNullOrEmpty(pId))
                            {
                                pId = Guid.NewGuid().ToString();
                            }
                            figureInfo.Captions.Add(pId, InitParagraph(figure.Caption.Paragraphs[i].InnerXml.Trim(), referenceIdPrefix, false, true));
                        }
                    }
                }
            }

            string imageFileName = System.IO.Path.GetFileName(figure.Graphic.Href).Replace(".tif", ".jpg");
            figureInfo.ImageUrls = new List<string>();

            string subImage1Name = System.IO.Path.GetFileNameWithoutExtension(imageFileName) + "-1" + System.IO.Path.GetExtension(imageFileName);

            if (this.isImageInDirectory)
            {
                var files = Directory.GetFiles(parameters.FileOutputDir, "*" + subImage1Name, SearchOption.AllDirectories);
                if (files != null && files.Count() == 1)
                {
                    figureInfo.ImageUrls.Add(files[0]);
                    for (int iIndex = 2; iIndex <= 10; iIndex++)
                    {
                        string subImageName = System.IO.Path.GetFileNameWithoutExtension(imageFileName) + "-" + iIndex + System.IO.Path.GetExtension(imageFileName);
                        files = Directory.GetFiles(parameters.FileOutputDir, "*" + subImageName, SearchOption.AllDirectories);

                        if (files != null && files.Count() == 1)
                        {
                            figureInfo.ImageUrls.Add(files[0]);

                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    files = Directory.GetFiles(parameters.FileOutputDir, "*" + imageFileName, SearchOption.AllDirectories);
                    if (files != null && files.Count() == 1)
                    {
                        figureInfo.ImageUrls.Add(files[0]);
                    }
                }
            }
            else
            {
                
            }

            

            return figureInfo;
        }

        private ListInfo GetArticleList(Mavercloud.JATS.Entities.ListWrapper list)
        {
            var listInfo = new ListInfo() { Items = new List<ListItemInfo>()};
            if (!string.IsNullOrEmpty(list.ListType))
            { 
                
            }
            foreach (var item in list.Items)
            {
                var itemInfo = new ListItemInfo();
                listInfo.Items.Add(itemInfo);
                if (item.Label != null && !string.IsNullOrEmpty(item.Label.InnerXml))
                {
                    itemInfo.Symbol = item.Label.InnerXml;
                }
                if (item.Elements != null && item.Elements.Count > 0)
                {
                    itemInfo.Text = InitParagraph((item.Elements[0] as JATS.Entities.Paragraph).InnerXml, referenceIdPrefix, false, true);
                }
            }
            return listInfo;
        }
        #endregion

        #region Init Elements
        private void InitializeAddingTables()
        {
            addingTables = new List<ArticleAddingTable>();
            foreach (var section in articleInfo.ContentItems)
            {
                CreateAddingTable(section);
            }
        }

        private void CreateAddingTable(SectionInfo section)
        {

            foreach (var child in section.Items)
            {
                if (child is FigureInfo)
                {
                    var figureInfo = child as FigureInfo;
                    if (figureInfo.ImageType == ImageType.Image || figureInfo.ImageType == ImageType.Scheme)
                    {
                        ArticleAddingTableStyleInfo styleInfo = null;
                        if (tableStyles != null && tableStyles.ContainsKey(figureInfo.Id))
                        {
                            styleInfo = tableStyles[figureInfo.Id];
                            if (styleInfo.ImageWidth.HasValue)
                            {
                                figureInfo.Width = styleInfo.ImageWidth.Value;
                            }

                            figureInfo.DrawAtPdfPage = styleInfo.DrawAtPage;
                            figureInfo.TableDisplayType = styleInfo.DisplayType;
                            if (!styleInfo.Position.HasValue)
                            {
                                styleInfo.Position = TablePosition.FollowingWithContent;
                            }

                        }
                        else
                        {
                            styleInfo = new ArticleAddingTableStyleInfo()
                            {
                                Position = TablePosition.FollowingWithContent
                            };
                        }
                        if (figureInfo.Captions != null && figureInfo.Captions.Count > 1)
                        {
                            sectionFigureCaptionItem.Items.First(t => t.Name == "FigureCaption").Items[0].TextAlignmentCenterForShortContent = false;
                        }
                        else
                        {
                            sectionFigureCaptionItem.Items.First(t => t.Name == "FigureCaption").Items[0].TextAlignmentCenterForShortContent = defaultShortFigureCaptionAlignCenter;
                        }
                        var imageTableHandler = new BizFigureTableHandler(figureInfo, sectionFigureItem, styleName, document, parameters.ParagraphStyles);
                        var imageTable = imageTableHandler.Handle();

                        var imageCaptionTableHandler = new BizFigureTableHandler(figureInfo, sectionFigureCaptionItem, styleName, document, parameters.ParagraphStyles);
                        var imageCaptionTable = imageCaptionTableHandler.Handle();

                        var tableHeight = imageTable.GetHeightOnRendering(pageWidth, 1000000f, document) + imageCaptionTable.GetHeightOnRendering(pageWidth, 1000000f, document);

                        var type = 1;
                        var number = 1;
                        if (addingTables.Any(t => t.Type == type))
                        {
                            number = addingTables.Where(t => t.Type == type).Max(t => t.Number) + 1;
                        }

                        addingTables.Add(new ArticleAddingTable()
                        {
                            Type = type,
                            Id = figureInfo.Id,
                            Number = number,
                            Elements = new List<BlockElement<iText.Layout.Element.Table>>() { imageTable, imageCaptionTable },
                            TableHeight = tableHeight,
                            DisplayType = figureInfo.TableDisplayType.HasValue ? figureInfo.TableDisplayType.Value : TableDisplayType.Normal,
                            DrawAtPdfPage = figureInfo.DrawAtPdfPage,
                            StyleInfo = styleInfo,
                            BodyItem = child,
                            MultiImages = figureInfo.ImageUrls.Count > 1

                        });
                    }
                    else if (figureInfo.ImageType == General.Entities.ImageType.Equation)
                    {
                        if (tableStyles != null && tableStyles.ContainsKey(figureInfo.Id))
                        {
                            var styleInfo = tableStyles[figureInfo.Id];
                            if (styleInfo.ImageWidth.HasValue)
                            {
                                figureInfo.Width = styleInfo.ImageWidth.Value;
                            }
                        }
                    }
                }
                else if (child is TableInfo)
                {
                    var tableInfo = child as TableInfo;
#if !DEBUG
                    try
                    {
#endif
                        ArticleAddingTableStyleInfo styleInfo = null;
                        if (tableStyles != null && tableStyles.ContainsKey(tableInfo.Id))
                        {
                            styleInfo = tableStyles[tableInfo.Id];
                            if (styleInfo.TableWidth.HasValue)
                            {
                                tableInfo.Width = styleInfo.TableWidth.Value;
                            }

                            if (tableInfo.Width.HasValue)
                            {
                                tableInfo.TableDisplayType = TableDisplayType.Normal;
                            }

                            tableInfo.DrawAtPdfPage = styleInfo.DrawAtPage;
                            if (styleInfo.DisplayType.HasValue)
                            {
                                tableInfo.TableDisplayType = styleInfo.DisplayType;
                            }
                            else
                            {
                                tableInfo.TableDisplayType = TableDisplayType.Normal;
                            }
                            tableInfo.TableType = styleInfo.TableType;

                            if (!styleInfo.Position.HasValue)
                            {
                                styleInfo.Position = TablePosition.FollowingWithContent;
                            }
                        }
                        else
                        {
                            styleInfo = new ArticleAddingTableStyleInfo() { Position = TablePosition.FollowingWithContent, DisplayType = TableDisplayType.Normal };
                        }

                        var innerXml = "<Table>" + tableInfo.InnerXml.Replace("> <", "><Space/><") + "</Table>";
                        XmlDocument contentDoc = new XmlDocument();
                        contentDoc.LoadXml(innerXml);

                        var tableHeadCells = contentDoc.ChildNodes[0].SelectNodes("//th");
                        foreach(XmlNode cell in tableHeadCells)
                        {
                            if (!string.IsNullOrEmpty(cell.InnerXml))
                            {
                                cell.InnerXml = cell.InnerXml.Replace("<bold>", "").Replace("</bold>", "");

                                if (!cell.InnerXml.StartsWith("<p>"))
                                {
                                    if (cell.InnerXml.Contains("<break />"))
                                    {
                                        cell.InnerXml = "<p>" + cell.InnerXml.Replace("<break />", "</p><p>") + "</p>";
                                    }
                                }
                                if (cell.InnerXml.StartsWith("<p>"))
                                {
                                    var innerXmlBuilder = new StringBuilder();
                                    var pNodes = cell.SelectNodes("p");
                                    foreach (XmlNode pnode in pNodes)
                                    {
                                        innerXmlBuilder.Append("<p>");
                                        innerXmlBuilder.Append(InitParagraph(pnode.InnerXml, referenceIdPrefix));
                                        innerXmlBuilder.Append("</p>");
                                    }
                                    cell.InnerXml = innerXmlBuilder.ToString();
                                }
                                else
                                {
                                    cell.InnerXml = InitParagraph(cell.InnerXml, referenceIdPrefix);
                                }


                                
                            }
                        }
                        var tableCells = contentDoc.ChildNodes[0].SelectNodes("//td");

                        var cellIndex = 0;
                        foreach (XmlNode cell in tableCells)
                        {
                            
                            if (!string.IsNullOrEmpty(cell.InnerXml))
                            {
                                if (!cell.InnerXml.StartsWith("<p>"))
                                {
                                    if (cell.InnerXml.StartsWith("<list"))
                                    {
                                        cell.InnerXml = "<p>" + cell.InnerXml + "</p>";
                                    }
                                    else if (cell.InnerXml.Contains("<break />"))
                                    {
                                        cell.InnerXml = "<p>" + cell.InnerXml.Replace("<break />", "</p><p>") + "</p>";
                                    }
                                }
                                if (cell.InnerXml.StartsWith("<p>"))
                                {
                                    var innerXmlBuilder = new StringBuilder();
                                    var pNodes = cell.SelectNodes("p");
                                    foreach (XmlNode pnode in pNodes)
                                    {
                                        innerXmlBuilder.Append("<p>");
                                        innerXmlBuilder.Append(InitParagraph(pnode.InnerXml, referenceIdPrefix, false, false, true));
                                        innerXmlBuilder.Append("</p>");
                                    }
                                    cell.InnerXml = innerXmlBuilder.ToString();
                                }
                                else
                                {
                                    cell.InnerXml = InitParagraph(cell.InnerXml, referenceIdPrefix, false, false, true);
                                }
                            }
                            cellIndex++;
                        }

                        if (styleInfo.TableCellParagraphPaddingBottom.HasValue)
                        {
                            tableBodyCellParaItem.PaddingBottom = styleInfo.TableCellParagraphPaddingBottom.Value;
                        }
                        else
                        {
                            tableBodyCellParaItem.PaddingBottom = defaultTableBodyCellParagraphPaddingBottom;
                        }
                        if (styleInfo.TableFontSize.HasValue)
                        {
                            tableBodyCellParaItem.FontSize = styleInfo.TableFontSize.Value;
                            tableHeaderParaItem.FontSize = styleInfo.TableFontSize.Value;

                            PDFHelpers.ModifyAllFontSize(tableBodyCellListItem, styleInfo.TableFontSize.Value);
                        }
                        else
                        {
                            tableBodyCellParaItem.FontSize = defaultTableBodyCellParagraphFontSize;
                            tableHeaderParaItem.FontSize = defaultTableHeaderCellParagraphFontSize;
                            PDFHelpers.ModifyAllFontSize(tableBodyCellListItem, defaultTableBodyCellParagraphFontSize.Value);
                        }
                        if (styleInfo.TableMultipliedLeading.HasValue)
                        {
                            tableBodyCellParaItem.MultipliedLeading = styleInfo.TableMultipliedLeading.Value;
                        }
                        else
                        {
                            tableBodyCellParaItem.MultipliedLeading = defaultTableBodyCellParagraphMultileading;
                        }
                        var articleTableHandler = new BizHtmlArticleTableDivHandler(tableInfo,
                            contentDoc.ChildNodes[0], sectionTableItem, referenceIdPrefix,
                            styleName, document,
                            tableInfo.TableType,
                            tableInfo.TableDisplayType, parameters.ParagraphStyles);

                        articleTableHandler.SetTextAlignCenterWhenHeaderMultipleColspan(true);
                        articleTableHandler.SetCellBottomBorderWhenHeaderMultipleColspan(true);
                        articleTableHandler.SetTableColumnMinWidths(styleInfo.TableColumnWidths);
                        articleTableHandler.SetEnabledMaxColumnCellWidthAsCellMinWidth(false);
                        Div articleTable = null;


                        articleTable = articleTableHandler.Handle();


                    var displayType = articleTableHandler.GetDisplayType();
                    var tableHeight = 0f;
                    if (articleTable.GetHeight() != null)
                    {
                        tableHeight = articleTable.GetHeight().GetValue();
                    }
                    else
                    {
                        var tableWidth = 0f;
                        if (tableInfo.Width.HasValue)
                        {
                            tableWidth = tableInfo.Width.Value;
                        }
                        else
                        {
                            if (displayType == TableDisplayType.Normal)
                            {
                                tableWidth = pageWidth;
                            }
                            else
                            {
                                tableWidth = pageHeightWithMargin - Constants.DocumentMargins[1] - Constants.DocumentMargins[3];
                            }
                        }

                        try
                        {
                            tableHeight = articleTable.GetHeightOnRendering(tableWidth, 1000000f, document);
                        }
                        catch
                        {
                            tableHeight = articleTable.GetHeightOnRendering(tableWidth, 1000000f, document);
                        }
                        //tableHeight = articleTable.GetHeightOnRendering(tableWidth, 1000000f, document);

                    }

                    var donotPrepare = false;
                    var rotation = articleTableHandler.GetRotation();

                    var addingTable = new ArticleAddingTable()
                    {
                        Type = 0,
                        Id = tableInfo.Id,
                        Number = System.Convert.ToInt32(tableInfo.Id.Substring(tableInfo.Id.LastIndexOf('t') + 1)),
                        Container = articleTable,
                        Table = articleTableHandler.ArticleTable,
                        TableHeight = tableHeight,
                        Rotation = rotation,
                        DisplayType = displayType,
                        DonotPrepare = donotPrepare,
                        DrawAtPdfPage = tableInfo.DrawAtPdfPage,
                        Width = tableInfo.Width,
                        StyleInfo = styleInfo,
                        BodyItem = child
                    };
                    if (!addingTable.Rotation && !addingTable.Width.HasValue)
                    {
                        addingTable.Table.UseAllAvailableWidth();
                    }

                    addingTables.Add(addingTable);
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error in Drawing " + tableInfo.Id + " " + ex.Message);
                    }
#endif


                }
                else if (child is SectionInfo)
                {
                    CreateAddingTable(child as SectionInfo);
                }
            }

        }

        private void InitializeArticleElements()
        {
            
            articleElements = new List<ArticleElement>();
            foreach (var section in articleInfo.ContentItems)
            {
                PrepareSectionElements(section, articleElements);
            }
        }

        private void InitializeBackElements()
        {
            backElements = new List<ArticleElement>();
            if (articleInfo.BackItems != null)
            {
                foreach (var section in articleInfo.BackItems)
                {
                    PrepareSectionElements(section, backElements, null, 0, true);
                }
            }
        }

        private void PrepareSectionElements(SectionInfo section,
            List<ArticleElement> theArticleElements, PdfOutlineItem parentOutlineItem = null,
            int sectionIndex = 0,
            bool isBackElement = false)
        {
            PdfOutlineItem currentOutlineItem = null;
            ArticleElement titleArticleElement = null;
            if (!string.IsNullOrEmpty(section.Title))
            {
                var titleStyleItem = sectionTitleItem.Items.Single(t => t.Name == "SectionTitle" + section.Level);


                var clonedItem = titleStyleItem;
                if (!string.IsNullOrEmpty(section.TitleId)
                        && parameters.ParagraphStyles != null
                        && parameters.ParagraphStyles.ContainsKey(section.TitleId))
                {
                    var paragraphStyle = parameters.ParagraphStyles[section.TitleId];
                    clonedItem = StyleHelper.GetElementItemByParagraphStyle(titleStyleItem, paragraphStyle);
                }

                var titleElement = ElementGenerator.GetBlockElement(section.Title, clonedItem, document, styleName) as iText.Layout.Element.Paragraph;

                var sectionId = section.Id;
                if (string.IsNullOrEmpty(sectionId))
                {
                    sectionId = GetRandomDestination();
                }
                else
                {
                    sectionId = referenceIdPrefix + section.Id;
                }

                titleElement.SetDestination(sectionId);
                currentOutlineItem = new PdfOutlineItem() { Id = sectionId, Text = GetXmlInnerText(section.Title), Destination = sectionId };
                if (parentOutlineItem == null)
                {
                    outlineItems.Add(currentOutlineItem);
                }
                else
                {
                    parentOutlineItem.AddItem(currentOutlineItem);
                }


                titleArticleElement = new ArticleElement() { Type = 1, TitleLevel = section.Level, Element = titleElement as iText.Layout.Element.Paragraph };
                if (section.Title == "Abbreviations" || section.Title == "Abbreviation")
                {
                    titleArticleElement.FollowingParagraphIndent = false;
                    titleArticleElement.IsAbbreviationsTitle = true;
                }
                else
                {
                    titleArticleElement.FollowingParagraphIndent = true;
                }
                theArticleElements.Add(titleArticleElement);
            }

            var contentItems = section.Items.Where(t => t is ParagraphInfo
            || t is SectionInfo
            || t is FigureInfo
            || t is TableInfo
            || t is JatsListInfo).ToList();
            int i = 0;
            foreach (var child in contentItems)
            {
                if (child is ParagraphInfo)
                {
                    var paraInfo = child as ParagraphInfo;

                    var clonedItem = sectionParaItem;

                    if (!string.IsNullOrEmpty(paraInfo.Id)
                        && parameters.ParagraphStyles != null
                        && parameters.ParagraphStyles.ContainsKey(paraInfo.Id))
                    {
                        var paragraphStyle = parameters.ParagraphStyles[paraInfo.Id];
                        clonedItem = StyleHelper.GetElementItemByParagraphStyle(sectionParaItem, paragraphStyle);
                    }
#if DEBUG
                    if (paraInfo.Text.Contains("Healthcare data management: Blockchain"))
                    { 
                        
                    }
                        

#endif
                    var paragraph = ElementGenerator.GetBlockElement(paraInfo.Text, clonedItem, document, styleName) as iText.Layout.Element.Paragraph;
                    if (titleArticleElement != null
                        && titleArticleElement.IsAbbreviationsTitle.GetValueOrDefault())
                    {
                        if (i == 0 || i == contentItems.Count - 2)
                        {
                            paragraph.SetKeepWithNext(true);
                        }
                    }
                    if (paraInfo.SetDestination && !string.IsNullOrEmpty(paraInfo.Id))
                    {
                        paragraph.SetDestination(paraInfo.Id);
                    }
                    var paraElement = new ArticleElement() { Type = 0, Element = paragraph, };
                    if (paraInfo.Text.StartsWith("where"))
                    {
                        paraElement.ExplicitNotParagraphIndent = true;
                    }
                    theArticleElements.Add(paraElement);
                }
                else if (child is JatsListInfo)
                {
                    var listInfo = child as JatsListInfo;
                    var jatsListHandler = new JatsListHandler(listInfo, sectionListItem, styleName, document);

                    theArticleElements.Add(new ArticleElement()
                    {
                        Type = 4,
                        ListElement = jatsListHandler.Handle()
                    });

                }
                else if (child is SectionInfo)
                {
                    PrepareSectionElements(child as SectionInfo, theArticleElements, currentOutlineItem, i, isBackElement);
                }
                else if (child is FigureInfo)
                {
                    var figureInfo = child as FigureInfo;
                    if (figureInfo.ImageType == ImageType.Equation && figureInfo.ImageUrls != null && figureInfo.ImageUrls.Count > 0)
                    {
                        var paragraphHandler = new ParagraphHandler(figureInfo, sectionEquationParaItem, styleName, document, true);
                        var paragraph = paragraphHandler.Handle();
                        var eqImageHandler = new ImageHandler(figureInfo.ImageUrls[0], sectionEquationItem, styleName, document, figureInfo.Width, figureInfo.Width);
                        paragraph.Add(eqImageHandler.Handle());
                        theArticleElements.Add(new ArticleElement() { Type = 0, Element = paragraph, });

                    }
                    else
                    {
                        theArticleElements.Add(new ArticleElement() { Type = 2, Id = figureInfo.Id });
                    }
                }
                else if (child is TableInfo)
                {
                    theArticleElements.Add(new ArticleElement() { Type = 3, Id = (child as TableInfo).Id });
                }
                i++;
            }
        }
        #endregion

        #region Draw Elements

        private bool IsSpacingEnoughForTable(ArticleAddingTable table)
        {
            bool enough = false;
            var bottomInterval = 23f;
            currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
            var remainYSpacing = currentYLine - Constants.DocumentMargins[2] - 1f;
            var tableHeight = 0f;
            try
            {
                if (table.Container != null)
                {
                    tableHeight = table.Container.GetHeightOnRendering(pageWidth, pageHeight, document);
                }
                else if (table.Table != null)
                {
                    tableHeight = table.Table.GetHeightOnRendering(pageWidth, pageHeight, document);
                }
                else
                {
                    tableHeight = table.TableHeight;
                }
            }
            catch
            {
                tableHeight = table.TableHeight;
            }
            enough = remainYSpacing > tableHeight + bottomInterval;
            
            return enough;
        }
        private bool IsSpacingEnoughForImageTable(ArticleAddingTable table)
        {
            bool enough = false;
            var bottomInterval = 23f;
            currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
            var remainYSpacing = currentYLine - Constants.DocumentMargins[2] - 1f;
            var tableHeight = table.TableHeight;

            enough = remainYSpacing > tableHeight + bottomInterval;
            if (!enough && table.Type == 1 && table.BodyItem is FigureInfo)
            {
                var imageContainerItem = sectionFigureItem.Items.First(t => t.Name == "FigureImage");

                var figureInfo = table.BodyItem as FigureInfo;
                if (!figureInfo.Width.HasValue)
                {
                    figureInfo.Width = pageWidth;
                }
                figureInfo.Width -= 20f;
                while (figureInfo.Width > 100f)
                {
                    var imageContainer = ElementGenerator.GetTable(figureInfo, imageContainerItem, document, styleName);
                    try
                    {
                        tableHeight = imageContainer.GetHeightOnRendering(pageWidth, pageHeight, document);

                        if (remainYSpacing > tableHeight + bottomInterval)
                        {
                            table.Elements[0] = imageContainer;
                            enough = true;
                            break;
                        }
                        else
                        {
                            figureInfo.Width -= 20f;
                        }
                    }
                    catch
                    {
                        break;
                    }

                    
                }
            }
            return enough;
        }

        private float? lastParagraphPaddingBottom;
        private bool? previousIsParagraph;
        private bool? newPageForFollowingTable;
        private int lastElementType;
        private void DrawArticleElements(List<ArticleElement> elements, bool followingWithTitle = false, bool isBack = false)
        {

            toDrawTableIds = new List<string>();
            var nextListIndent = 0f;
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (startNewPageForLimitedSpace)
                {
                    DrawTables();
                    startNewPageForLimitedSpace = false;
                    newPageForFollowingTable = true;
                }
                else
                {
                    newPageForFollowingTable = false;
                }
                if (element.Type == 1)
                {

                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                    if (element.TitleLevel == 1)
                    {
                        setFirstLineIndent = element.FollowingParagraphIndent.GetValueOrDefault(true);
                    }

                    if (previousIsParagraph.GetValueOrDefault() && lastParagraphPaddingBottom.HasValue && lastParagraphPaddingBottom.Value < 10f)
                    {
                        var tPaddingTop = 0f;
                        if (element.TitleLevel == 1)
                        {
                            tPaddingTop = 13f - lastParagraphPaddingBottom.GetValueOrDefault();
                        }
                        else
                        {
                            tPaddingTop = 11f - lastParagraphPaddingBottom.GetValueOrDefault();
                        }
                        if (tPaddingTop > 0)
                        {
                            element.Element.SetPaddingTop(tPaddingTop);
                        }
                    }

                    SplittingArticleParagraphRenderer paragraphRenderer = new SplittingArticleParagraphRenderer(element.Element, addingTables);
                    paragraphRenderer.InitTableLinks(element.Element);
                    var toDrawTableLinkIds = paragraphRenderer.GetToDrawTableLinkIds();
                    if (toDrawTableLinkIds != null && toDrawTableLinkIds.Count > 0)
                    {
                        foreach (var linkId in toDrawTableLinkIds)
                        {
                            var at = addingTables.First(t => t.Id == linkId);
                            if (!toDrawTableIds.Contains(linkId) && at.Drawn == false && !at.DrawAtPdfPage.HasValue)
                            {
                                toDrawTableIds.Add(linkId);
                            }
                        }
                    }
                    headerEventHandler.SetCurrentParagraph(element.Element);
                    document.Add(element.Element);
                    previousIsParagraph = false;
                    lastElementType = element.Type;
                }
                else if (element.Type == 0)
                {

                    var paragraph = element.Element;


                    if (paragraph.GetChildren()[0] is Image)
                    {
                        document.Add(paragraph);
                        previousIsParagraph = false;
                    }
                    else
                    {
                        if (i > 0 
                            && !element.ExplicitNotParagraphIndent.GetValueOrDefault()
                            && elements[i - 1].Type != 1 
                            && setFirstLineIndent)
                        {
                            paragraph.SetFirstLineIndent(Constants.ParagraphFirstLineIndent);
                            nextListIndent = Constants.ParagraphFirstLineIndent + Constants.ListIndent;
                        }
                        else
                        {
                            nextListIndent = Constants.ParagraphFirstLineIndent;
                        }

                        SetArticleElementPaddingBottom(elements, i, paragraph);

                        paragraph.SetNextRenderer(new SplittingArticleParagraphRenderer(paragraph, addingTables).SetParent(document.GetRenderer()));
                        var paragraphRenderer = (SplittingArticleParagraphRenderer)paragraph.CreateRendererSubTree();
                        DrawElementRenderer(paragraphRenderer, i, false);
                    }

                    lastElementType = element.Type;
                }
                else if (element.Type == 2 || element.Type == 3)
                {
                    var table = addingTables.FirstOrDefault(t => t.Id == element.Id);
                    if (table != null && table.StyleInfo != null
                        && table.StyleInfo.Position == TablePosition.FollowingWithContent
                        && table.DisplayType != TableDisplayType.PageRotation
                        && !table.Drawn)
                    {
                        if (element.Type == 3 || table.MultiImages || IsSpacingEnoughForTable(table))
                        {
                            var orderedDrawing = true;
                            if (element.Type == 2 && table.Number > 1)
                            {
                                var lastTable = addingTables.Where(t => t.Number == table.Number - 1 && t.Type == table.Type).First();
                                if (!lastTable.Drawn)
                                {
                                    table.StyleInfo.Position = TablePosition.TopOfPage;
                                    orderedDrawing = false;
                                }
                            }
                            if (orderedDrawing)
                            {
                                if (table.Elements != null && table.Elements.Count > 0)
                                {
                                    if (!newPageForFollowingTable.GetValueOrDefault())
                                    {
                                        if (element.Type == 2 && (elements[i - 1].Type == 0 || elements[i - 1].Type == 4 || previousIsParagraph.GetValueOrDefault()))
                                        {
                                            table.Elements[0].SetMarginTop(20f);
                                        }
                                        else if (element.Type == 3 && (elements[i - 1].Type == 0 || elements[i - 1].Type == 4))
                                        {
                                            table.Elements[0].SetMarginTop(13f);
                                        }
                                    }
                                    table.Elements.ForEach(t => document.Add(t));
                                }
                                else if (table.Container != null)
                                {
                                    if (!newPageForFollowingTable.GetValueOrDefault())
                                    {
                                        if (element.Type == 2 && (lastElementType == 0 || lastElementType == 4))
                                        {
                                            table.Container.SetMarginTop(20f);
                                        }
                                        else if (element.Type == 3 && (lastElementType == 0 || lastElementType == 4))
                                        {
                                            table.Container.SetMarginTop(13f);
                                        }
                                    }
                                    
                                    document.Add(table.Container);
                                }
                                else
                                {
                                    if (!newPageForFollowingTable.GetValueOrDefault())
                                    {
                                        if (element.Type == 2 && (lastElementType == 0 || lastElementType == 4))
                                        {
                                            table.Table.SetMarginTop(20f);
                                        }
                                        else if (element.Type == 3 && (lastElementType == 0 || lastElementType == 4))
                                        {
                                            table.Table.SetMarginTop(13f);
                                        }
                                    }
                                    
                                    document.Add(table.Table);
                                }
                                table.Drawn = true;
                                previousIsParagraph = false;
                                lastElementType = element.Type;
                            }
                        }
                        else
                        {
                            table.StyleInfo.Position = TablePosition.TopOfPage;
                            if (elements.Count > i + 1)
                            {
                                var nextIndex = i + 1;
                                while (nextIndex < elements.Count)
                                { 
                                    var nextElement = elements[nextIndex];
                                    if (nextElement.Type == 2 || nextElement.Type == 3)
                                    {
                                        var nextTable = addingTables.FirstOrDefault(t => t.Id == element.Id);
                                        nextTable.StyleInfo.Position = TablePosition.TopOfPage;
                                        nextIndex++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            
                        }
                        
                    }
                    newPageForFollowingTable = false;
                }
                else if (element.Type == 4)
                {
                    if (i + 1 >= elements.Count || elements[i + 1].Type == 1)
                    {
                        if (i + 1 >= elements.Count || elements[i + 1].TitleLevel == 1)
                        {
                            element.ListElement.SetMarginBottom(13f);
                        }
                        else
                        {
                            element.ListElement.SetMarginBottom(11f);
                        }
                    }
                    

                    var listTableLinkHelper = new ListTableLinkHelper(element.ListElement, addingTables);
                    var toDrawTableLinkIds = listTableLinkHelper.GetTableLinks();
                    if (toDrawTableLinkIds != null && toDrawTableLinkIds.Count > 0)
                    {
                        foreach (var linkId in toDrawTableLinkIds)
                        {
                            var at = addingTables.First(t => t.Id == linkId);
                            if (!toDrawTableIds.Contains(linkId) && at.Drawn == false && !at.DrawAtPdfPage.HasValue)
                            {
                                toDrawTableIds.Add(linkId);
                            }
                        }
                    }

                    document.Add(element.ListElement);


                    previousIsParagraph = false;
                    lastElementType = element.Type;
                }
                if (elements.Count > i + 1)
                {
                    var titleHeight = 0f;

                    var startIx = i + 1;
                    var nextIsTitle = false;

                    for (var ix = i + 1; ix < elements.Count; ix++)
                    {
                        if (elements[ix].Type == 1)
                        {
                            nextIsTitle = true;
                            startIx = ix;
                            break;
                        }
                        else if (elements[ix].Type == 0)
                        {
                            nextIsTitle = false;
                            break;
                        }
                    }

                    if (nextIsTitle)
                    {
                        for (int ix = startIx; ix < elements.Count; ix++)
                        {
                            if (elements[ix].Type == 1)
                            {
                                titleHeight += elements[ix].Element.GetHeightOnRendering(pageWidth, pageHeight, document);
                            }
                            else
                            {
                                break;
                            }
                        }
                        RemoveMarginIfStartingPage(nextIsTitle, titleHeight, isBack);
                    }
                    else
                    {
                        RemoveMarginIfStartingPage(nextIsTitle, null, isBack);
                    }
                }
                else
                {
                    if (!isBack)
                    {
                        if (backElements.Count > 0 || (articleInfo.Refs != null && articleInfo.Refs.Count > 0))
                        {
                            RemoveMarginIfStartingPage(true, null, false);
                        }
                        if (startNewPageForLimitedSpace)
                        {
                            DrawTables();
                            startNewPageForLimitedSpace = false;
                            newPageForFollowingTable = true;
                        }
                    }
                }
            }
        }

        private void SetArticleElementPaddingBottom<T>(List<ArticleElement> elements, int i, iText.Layout.Element.BlockElement<T> element) where T : IBlockElement
        {
            if (i < elements.Count - 1)
            {
                if (elements[i + 1].Type == 1)
                {
                    if (elements[i + 1].TitleLevel == 1)
                    {
                        element.SetPaddingBottom(13f);
                    }
                    else
                    {
                        element.SetPaddingBottom(11f);
                    }
                }
                else if (elements[i + 1].Type == 2 || elements[i + 1].Type == 3)
                {
                    var table = addingTables.FirstOrDefault(t => t.Id == elements[i + 1].Id);
                    if (table != null && table.StyleInfo != null
                        && table.StyleInfo.Position == TablePosition.FollowingWithContent
                        && !table.Drawn)
                    {
                        //element.SetPaddingBottom(13f);
                    }
                }
            }
            else
            {
                element.SetPaddingBottom(13f);
            }
        }

        private void RemoveMarginIfStartingPage(bool nextIsTitle, float? titleHeight = null, bool isBack = false)
        {
            startNewPageForLimitedSpace = false;
            currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
            currentPageNumber = document.GetRenderer().GetCurrentArea().GetPageNumber();

            var newPageNumber = currentPageNumber + 1;
            if (currentYLine >= Constants.PageTop)
            {
                newPageNumber = currentPageNumber;
            }

            var tableContinued = (toDrawTableIds != null && addingTables.Any(t => toDrawTableIds.Contains(t.Id) && t.Drawn == false))
                        || (addingTables != null && addingTables.Any(t => t.DrawAtPdfPage == newPageNumber && t.Drawn == false));

            ArticleAddingTable table = null;
            if (tableContinued)
            {
                table = addingTables.FirstOrDefault(t => t.DrawAtPdfPage == newPageNumber && t.Drawn == false);
                if (table == null)
                {
                    table = addingTables.FirstOrDefault(t => toDrawTableIds.Contains(t.Id) && t.Drawn == false);
                }
                
            }
            var bottomMargin = Constants.FirstPageBottomMargin;
            if (articleInfo.ContinuousPublish.GetValueOrDefault())
            {
                bottomMargin = Constants.FirstPageBottomMarginForContinuousPublish;
            }
            if (newPageNumber > 1)
            {
                bottomMargin = document.GetBottomMargin();
            }
            if (nextIsTitle && (!tableContinued || (table.StyleInfo != null 
                && table.StyleInfo.Position == TablePosition.TopOfPage)))
            {
                if (!titleHeight.HasValue)
                {
                    titleHeight = 36f;
                }
                if (currentYLine < bottomMargin + titleHeight)
                {
                    if (currentYLine > bottomMargin + 15)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                    }
                    startNewPageForLimitedSpace = true;
                    previousIsParagraph = false;
                }
                else
                {
                    if (isBack)
                    {
                        if (currentYLine < bottomMargin + titleHeight.Value + 25f)
                        {
                            //if (!tableContinued)
                            {
                                document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                            }
                            startNewPageForLimitedSpace = true;
                            previousIsParagraph = false;
                        }
                    }
                    else
                    {
                        if (currentYLine < bottomMargin + titleHeight.Value + 31f)
                        {
                            if (!tableContinued || table.DisplayType != TableDisplayType.PageRotation)
                            {
                                if (table != null && table.StyleInfo != null && table.StyleInfo.Position == TablePosition.FollowingWithContent)
                                {
                                    table.StyleInfo.Position = TablePosition.TopOfPage;
                                }
                                document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                            }
                            startNewPageForLimitedSpace = true;
                            previousIsParagraph = false;
                        }
                    }

                }
            }
            else if (!isBack)
            {
                var standardHeight = bottomMargin + 40f;
                if (currentYLine > bottomMargin + 15f && currentYLine < bottomMargin + 33f && tableContinued)
                {
                    if (table.DisplayType != TableDisplayType.PageRotation)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                    }
                    startNewPageForLimitedSpace = true;
                }
                else if ((currentYLine < standardHeight && currentYLine > bottomMargin + 10f) || Math.Abs(standardHeight - currentYLine) < 1)
                {
                    if (!tableContinued)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                    }
                    else
                    {
                        if (table == null || table.DisplayType == TableDisplayType.Normal)
                        {
                            if (standardHeight - currentYLine < 2 && standardHeight - currentYLine > 0)
                            {
                                document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                            }
                        }
                    }
                    startNewPageForLimitedSpace = true;
                }
                else if (currentYLine < bottomMargin + 15f)
                {
                    startNewPageForLimitedSpace = true;
                }
            }
        }

        public bool DrawTables()
        {
            bool drawed = false;
            if (!abstractGraphicDrawn && abstractGraphicInfo != null)
            {
                DrawAbstractGraphic();
            }
            if (toDrawTableIds != null)
            {
                currentPageNumber = document.GetRenderer().GetCurrentArea().GetPageNumber();
                currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();

                var newPageNumber = currentPageNumber + 1;
                if (currentYLine >= Constants.PageTop)
                {
                    newPageNumber = currentPageNumber;
                }

                var preparingObjects = addingTables.Where(t => (toDrawTableIds.Contains(t.Id) || t.DrawAtPdfPage == newPageNumber)
                    && (t.StyleInfo.Position == TablePosition.TopOfPage || t.DisplayType == TableDisplayType.PageRotation) && t.Drawn == false).ToList();
                if (preparingObjects.Count > 0)
                {
                    var heightList = new List<float>();
                    var drawingObjects = new List<ArticleAddingTable>();
                    var currentPageHeight = pageHeight;
                    var currentPageWidth = pageWidth;

                    drawingObjects = preparingObjects;
                    if (drawingObjects.Count > 0)
                    {
                        
                        for (int di = 0; di < drawingObjects.Count; di++)
                        {
                            var drawingObj = drawingObjects[di];
                            currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                            if (drawingObj.DisplayType == TableDisplayType.Normal)
                            {

                                if (currentYLine > document.GetBottomMargin() + 10 && currentYLine < document.GetBottomMargin() + 53)
                                {
                                    document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                }
                            }
                            else
                            {
                                if (di == 0 || (di > 0 && drawingObjects[di - 1].DisplayType == TableDisplayType.Normal))
                                {
                                    document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                                }
                            }

                            if (!drawed)
                            {
                                drawed = true;
                            }
                            drawingObj.Drawn = true;
                            startNewPageForLimitedSpace = false;
                            if (drawingObj.DisplayType == TableDisplayType.Normal)
                            {
                                
                                if (drawingObj.Elements != null && drawingObj.Elements.Count > 0)
                                {
                                    drawingObj.Elements.ForEach(t => document.Add(t));
                                }
                                else if (drawingObj.Container != null)
                                {
                                    document.Add(drawingObj.Container);
                                }
                                else if (drawingObj.Table != null)
                                {

                                    document.Add(drawingObj.Table);
                                }
                                
                                if (di == drawingObjects.Count - 1)
                                {
                                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                    var distance = currentYLine - document.GetBottomMargin();
                                    if (distance > 14f && distance < 40f)
                                    {
                                        startNewPageForLimitedSpace = true;
                                        document.Add(new AreaBreak());
                                    }
                                }
                            }
                            else
                            {
                                if (drawingObj.Container != null)
                                {
                                    drawingObj.Container.SetNextRenderer(new SplittingDivRenderer(drawingObj.Container).SetParent(document.GetRenderer()));
                                    var divRenderer = (SplittingDivRenderer)drawingObj.Container.CreateRendererSubTree();
                                    document.GetRenderer().AddChild(divRenderer);

                                    var leftOverIndex = 0;
                                    var tLeftOver = divRenderer.Leftover;
                                    while (tLeftOver != null)
                                    {
                                        document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                                        document.GetRenderer().AddChild(tLeftOver.SetParent(document.GetRenderer()));
                                        tLeftOver = tLeftOver.Leftover;
                                        leftOverIndex++;
                                        if (leftOverIndex > 10)
                                        {
                                            throw new Exception("Error in drawing " + drawingObj.Id);
                                        }
                                    }
                                }
                                else if (drawingObj.Table != null)
                                {
                                    drawingObj.Table.SetNextRenderer(new SplittingTableRenderer(drawingObj.Table).SetParent(document.GetRenderer()));
                                    var tableRenderer = (SplittingTableRenderer)drawingObj.Table.CreateRendererSubTree();
                                    document.GetRenderer().AddChild(tableRenderer);

                                    var tLeftOver = tableRenderer.Leftover;
                                    while (tLeftOver != null)
                                    {
                                        var continueRotation = true;
                                        
                                        if (continueRotation)
                                        {
                                            document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                                        }
                                        else
                                        {
                                            document.Add(new AreaBreak());

                                        }
                                        document.GetRenderer().AddChild(tLeftOver.SetParent(document.GetRenderer()));
                                        tLeftOver = tLeftOver.Leftover;
                                    }
                                }
                                else if (drawingObj.Elements != null && drawingObj.Elements.Count == 2)
                                {
                                    var imageTable = drawingObj.Elements[0] as iText.Layout.Element.Table;
                                    imageTable.SetNextRenderer(new SplittingTableRenderer(imageTable).SetParent(document.GetRenderer()));
                                    var tableRenderer = (SplittingTableRenderer)imageTable.CreateRendererSubTree();
                                    document.GetRenderer().AddChild(tableRenderer);

                                    var tLeftOver = tableRenderer.Leftover;
                                    while (tLeftOver != null)
                                    {
                                        document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                                        document.GetRenderer().AddChild(tLeftOver.SetParent(document.GetRenderer()));
                                        tLeftOver = tLeftOver.Leftover;
                                    }

                                    var imageCaptionTable = drawingObj.Elements[1] as iText.Layout.Element.Table;
                                    if (imageCaptionTable != null)
                                    {
                                        currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                        var currInterval = currentYLine - document.GetBottomMargin();
                                        if (currInterval < 120f)
                                        {
                                            document.Add(new AreaBreak());
                                        }
                                        document.Add(imageCaptionTable);
                                    }
                                }


                                if (di == drawingObjects.Count - 1)
                                {
                                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                    var currInterval = currentYLine - document.GetBottomMargin();
                                    if (currInterval > 33.3f && currInterval < 63.3f)
                                    {
                                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                        startNewPageForLimitedSpace = true;
                                    }
                                }
                                else if (drawingObjects[di + 1].DisplayType == TableDisplayType.Normal)
                                {
                                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                    
                                    if (currentYLine > 80f && currentYLine < 120f)
                                    {
                                        document.Add(new AreaBreak());
                                        startNewPageForLimitedSpace = true;
                                    }

                                }
                                else if (drawingObjects[di + 1].DisplayType == TableDisplayType.PageRotation)
                                {
                                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                    var currInterval = currentYLine - document.GetBottomMargin();
                                    if (currInterval < 80f)
                                    {
                                        document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                                        startNewPageForLimitedSpace = true;
                                    }

                                }
                            }

                            if (di != drawingObjects.Count - 1)
                            {
                                if (drawingObjects[di + 1].DisplayType != drawingObj.DisplayType)
                                {
                                    if (!startNewPageForLimitedSpace)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                                    if (currentYLine > 120f && drawingObjects[di + 1].Type != 0 && drawingObjects[di + 1].TableHeight > currentYLine - document.GetBottomMargin())
                                    {
                                        break;
                                    }
                                }
                            }


                        }




                    }
                }
            }
            if (drawed)
            {
                previousIsParagraph = false;
            }
            return drawed;
        }

        
        private void DrawElementRenderer(SplittingArticleParagraphRenderer paragraphRenderer,
            int index,
            bool isLeftOver = false)
        {
            currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
            
            startNewPageForLimitedSpace = false;
            headerEventHandler.SetCurrentParagraph(paragraphRenderer.GetModelElement() as iText.Layout.Element.Paragraph);
            document.GetRenderer().AddChild(paragraphRenderer);

            var paddingBottom = (paragraphRenderer.GetModelElement() as iText.Layout.Element.Paragraph).GetPaddingBottom();
            lastParagraphPaddingBottom = paddingBottom.GetValue();
            previousIsParagraph = true;

            paragraphRenderer.InitTableLinks();
            var toDrawTableLinkIds = paragraphRenderer.GetToDrawTableLinkIds();
            if (toDrawTableLinkIds != null && toDrawTableLinkIds.Count > 0)
            {
                foreach (var linkId in toDrawTableLinkIds)
                {
                    var at = addingTables.First(t => t.Id == linkId);
                    if (!toDrawTableIds.Contains(linkId) && at.Drawn == false && !at.DrawAtPdfPage.HasValue)
                    {
                        toDrawTableIds.Add(linkId);
                    }
                }
            }


            var leftOver = paragraphRenderer.GetLeftOver();

            var drawTables = false;

            if (leftOver != null)
            {
                drawTables = true;
            }

            if (drawTables)
            {
                DrawTables();
            }
            if (leftOver != null)
            {
                DrawElementRenderer(leftOver.SetParent(document.GetRenderer()) as SplittingArticleParagraphRenderer, index, true);
            }
        }

        private void DrawRemainTables()
        {
            //继续绘制未完成的表格
            var unDrawnTables = addingTables.Where(t => t.Drawn == false).OrderBy(t => t.Type).ToList();
            if (unDrawnTables != null && unDrawnTables.Count > 0)
            {
                if (unDrawnTables[0].DisplayType == TableDisplayType.Normal)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                }
                else
                {
                    document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                }


                var displayType = unDrawnTables[0].DisplayType;

                foreach (var unDrawnTable in unDrawnTables)
                {
                    if (unDrawnTable.DisplayType != displayType)
                    {
                        if (unDrawnTable.DisplayType == TableDisplayType.PageRotation)
                        {
                            document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                        }
                        else
                        {
                            document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetWidth(), Constants.DefaultPageSize.GetHeight())));

                        }
                        displayType = unDrawnTable.DisplayType;
                    }
                    if (unDrawnTable.DisplayType == TableDisplayType.PageRotation)
                    {
                        unDrawnTable.Container.SetNextRenderer(new SplittingDivRenderer(unDrawnTable.Container).SetParent(document.GetRenderer()));
                        var divRenderer = (SplittingDivRenderer)unDrawnTable.Container.CreateRendererSubTree();
                        document.GetRenderer().AddChild(divRenderer);

                        var tLeftOver = divRenderer.Leftover;
                        while (tLeftOver != null)
                        {
                            document.Add(new AreaBreak(new PageSize(Constants.DefaultPageSize.GetHeight(), Constants.DefaultPageSize.GetWidth())));
                            document.GetRenderer().AddChild(tLeftOver.SetParent(document.GetRenderer()));
                            tLeftOver = tLeftOver.Leftover;
                        }
                    }
                    else
                    {
                        if (unDrawnTable.Elements != null && unDrawnTable.Elements.Count > 0)
                        {
                            unDrawnTable.Elements.ForEach(t => document.Add(t));
                        }
                        else if (unDrawnTable.Container != null)
                        {
                            document.Add(unDrawnTable.Container);
                        }
                        else
                        {
                            document.Add(unDrawnTable.Table);
                        }
                    }
                }

                currentYLine = document.GetRenderer().GetCurrentArea().GetBBox().GetTop();
                if (currentYLine > 80f && currentYLine < 120f)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                }
            }
        }

        private T AddTitleDestinationAndOutline<T>(string titleId, string text,
           ElementPropertyContainer<T> element, PdfOutlineItem parentOutline = null) where T : IPropertyContainer
        {
            var outline = new PdfOutlineItem()
            {
                Id = titleId,
                Text = text,
                Destination = titleId
            };
            if (parentOutline == null)
            {
                outlineItems.Add(outline);
            }
            else
            {
                parentOutline.AddItem(outline);
            }
            return element.SetDestination(titleId);
        }

        private void AddOutlines(PdfOutlineItem item, PdfOutline parentOutline)
        {
            PdfOutline currentOutline = parentOutline.AddOutline(item.Text);
            currentOutline.AddAction(PdfAction.CreateGoTo(item.Destination));
            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var childItem in item.Items)
                {
                    AddOutlines(childItem, currentOutline);
                }
            }

        }

#endregion


        #region Static Methods
        private static string GetDateStr(Date date)
        {
            var dateTime = DateTime.Parse(string.Format("{0}-{1}-{2}", date.Year.InnerXml, date.Month.InnerXml, date.Day.InnerXml));
            var inf = new CultureInfo("en-US", false);
            return dateTime.ToString("MMMMMMMMMMMMM d, yyyy", inf);
        }

        private static string GetPubDateStr(PublicationDate date)
        {
            var dateTime = DateTime.Parse(string.Format("{0}-{1}-{2}", date.Year.InnerXml, date.Month.InnerXml, date.Day.InnerXml));
            var inf = new CultureInfo("en-US", false);
            return dateTime.ToString("MMMMMMMMMMMMM d, yyyy", inf);
        }

        private static string GetElocationId(string firstPage, string lastPage)
        {
            if (firstPage == lastPage)
            {
                return firstPage;
            }
            else
            {
                if (firstPage.Length == lastPage.Length && !firstPage.Contains(".") && !lastPage.Contains("."))
                {
                    var subIndex = 0;
                    for (int ci = 0; ci < firstPage.Length; ci++)
                    {
                        if (firstPage[ci] != lastPage[ci])
                        {
                            subIndex = ci;
                            break;
                        }
                    }
                    if (subIndex > 0)
                    {
                        lastPage = lastPage.Substring(subIndex);
                    }
                }
                return firstPage + "–" + lastPage;
            }
        }


        private static string GetRandomDestination()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        private static string GetXmlInnerText(string xml)
        {
            var textXml = "<Paragraph>" + TextHelper.InitXml(xml) + "</Paragraph>";
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(textXml);

            var rootNode = contentDoc.ChildNodes[0];
            return rootNode.InnerText;
        }

        private static string GetAnchorXml(string type, string destination, string innerXml, bool isSup = false)
        {
            string nodeName = "Anchor";
            if (type == RefType.Table)
            {
                nodeName = "TableAnchor";
            }
            else if (type == RefType.Figure)
            {
                nodeName = "FigureAnchor";
            }
            
            if (type == "sec" && !innerXml.Contains("/>"))
            {
                var builder = new StringBuilder();
                var words = innerXml.Split(' ');
                int index = 0;
                foreach (var t in words)
                {
                    if (index != words.Length - 1)
                    {
                        builder.Append(string.Format("<{0} Type=\"{1}\" Destination=\"{2}\">{3}</{0}>", nodeName, type, destination, t + " "));
                    }
                    else
                    {
                        builder.Append(string.Format("<{0} Type=\"{1}\" Destination=\"{2}\">{3}</{0}>", nodeName, type, destination, t));
                    }
                    index++;
                }
                return builder.ToString();
            }
            else if (type == RefType.Footnote)
            {
                nodeName = "FootnoteAnchor";
                return string.Format("<{0} Type=\"{1}\" Destination=\"{2}\">{3}</{0}>", nodeName, type, destination, innerXml.Replace("<Superscript>", "").Replace("</Superscript>", ""));
            }
            else
            {
                return string.Format("<{0} Type=\"{1}\" Destination=\"{2}\">{3}</{0}>", nodeName, type, destination, innerXml);
            }
        }

        private static string HandleWordsTag(string paragraph)
        {
            var paraXml = paragraph;

            var rootNode = TransferToNode(paraXml);
            List<XmlNode> handledNodes = new List<XmlNode>();
            foreach (XmlNode textNode in rootNode.ChildNodes)
            {
                if (textNode.NodeType == XmlNodeType.Element
                    && (textNode.Name.ToLower() == "italic"))
                {
                    var textOuterXml = textNode.OuterXml;
                    var replacedXml = string.Empty;
                    var text = textNode.InnerXml;
                    if (text.Contains(" ") && !text.Contains("</"))
                    {
                        var index = text.LastIndexOf(" ");
                        replacedXml = string.Format("<{0}>{1} </{0}><{0}>{2}</{0}>", textNode.Name, text.Substring(0, index), text.Substring(index + 1));
                        paraXml = paraXml.Replace(textOuterXml, replacedXml);
                    }
                }
            }
            return paraXml;
        }

        private static XmlNode TransferToNode(string xml)
        {
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml("<Paragraph>" + xml + "</Paragraph>");

            var rootNode = contentDoc.ChildNodes[0];
            return rootNode;
        }

        #endregion

        #region Handle Paragraph

        private static List<string> bracketsBeforeTag = new List<string>() { "[-]", "(-)" };

        
        private static string InitParagraph(string paragraph, 
            string referenceIdPrefix,
            bool isTable = false, 
            bool processEndingWord = false, 
            bool isTableCell = false,
            bool breakUrl = false)
        {
            if (!string.IsNullOrEmpty(paragraph))
            {
                paragraph = paragraph.Replace("\n", "");
                
                paragraph = paragraph.Replace("<italic>[<xref", "[<xref");
                paragraph = paragraph.Replace("</xref>]</italic>", "</xref>]");
                paragraph = paragraph.Replace("<institution>", "");
                paragraph = paragraph.Replace("</institution>", "");
                paragraph = paragraph.Replace("<addr-line>", "");
                paragraph = paragraph.Replace("</addr-line>", "");
                paragraph = paragraph.Replace("<grant-sponsor>", "");
                paragraph = paragraph.Replace("</grant-sponsor>", "");
                paragraph = paragraph.Replace("<grant-num>", "");
                paragraph = paragraph.Replace("</grant-num>", "");
                paragraph = paragraph.Replace("<ext-link ", "<uri ");
                paragraph = paragraph.Replace("</ext-link>", "</uri>");
                paragraph = paragraph.Replace("<inline-supplementary-material ", "<uri ");
                paragraph = paragraph.Replace("</inline-supplementary-material>", "</uri>");

                //paragraph = paragraph.Replace("<sup>[", "[");
                //paragraph = paragraph.Replace("]</sup>", "]");

                paragraph = paragraph.Replace("</xref>] </sup>", "</xref>]</sup> ");

                paragraph = paragraph.Replace("<sup>****</sup>", "<AsteriskSup>****</AsteriskSup>");
                paragraph = paragraph.Replace("<sup>***</sup>", "<AsteriskSup>***</AsteriskSup>");
                paragraph = paragraph.Replace("<sup>**</sup>", "<AsteriskSup>**</AsteriskSup>");
                paragraph = paragraph.Replace("<sup>*</sup>", "<AsteriskSup>*</AsteriskSup>");
                paragraph = paragraph.Replace("<sup>*", "<AsteriskSup>*</AsteriskSup><sup>");
                paragraph = paragraph.Replace("*</sup>", "</sup><AsteriskSup>*</AsteriskSup>");
                paragraph = paragraph.Replace("<italic> <sub>", "<Space/><italic><sub>");

                paragraph = paragraph.Replace(" bi-, ", " <InlineParagraph>bi-,</InlineParagraph> ");
                paragraph = paragraph.Replace(" tri-, ", " <InlineParagraph>tri-,</InlineParagraph> ");


                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"(?is)<a[^>]*?href=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>(?<text>(?:(?!</?a\b).)*)</a>");
                MatchCollection hrefMC = reg.Matches(paragraph);
                if (hrefMC != null && hrefMC.Count > 0)
                {
                    foreach (Match m in hrefMC)
                    {
                        string url = m.Groups["url"].Value;
                        string validUrl = url.ToLower();
                        if (!validUrl.StartsWith("http") && !validUrl.StartsWith("mailto"))
                        {
                            validUrl = "http://" + validUrl;
                            paragraph = paragraph.Replace(url, validUrl);
                        }
                    }
                }



                int i = -1;
                i = paragraph.IndexOf("<email>");
                while (i >= 0)
                {
                    int j = paragraph.IndexOf("</email>");
                    if (j > i)
                    {
                        string emailText = paragraph.Substring(i, j - i + 8);
                        string hrefText = "";
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(emailText);
                        XmlNode node = xml.SelectSingleNode("email");

                        if (node.InnerText.Contains("@") && paragraph.Contains("(" + emailText))
                        {
                            emailText = "(" + emailText;
                            var firstPart = node.InnerText.Substring(0, node.InnerText.IndexOf("@"));
                            var firstHrefText = "<InlineParagraph>(<WebSite href='mailto:" + node.InnerText + "'>" + firstPart + "</WebSite></InlineParagraph>";
                            var lastPart = node.InnerText.Substring(node.InnerText.IndexOf("@"));
                            var lastHrefText = "<WebSite href='mailto:" + node.InnerText + "'>" + lastPart + "</WebSite>";
                            if (paragraph.Contains(emailText + ")."))
                            {
                                lastHrefText = "<InlineParagraph>" + lastHrefText + ").</InlineParagraph>";
                                emailText = emailText + ").";
                            }
                            else if (paragraph.Contains(emailText + ")"))
                            {
                                lastHrefText = "<InlineParagraph>" + lastHrefText + ")</InlineParagraph>";
                                emailText = emailText + ")";
                            }
                            paragraph = paragraph.Replace(emailText, firstHrefText + lastHrefText);
                        }
                        else
                        {
                            hrefText = "<WebSite href='mailto:" + node.InnerText + "'>" + node.InnerText + "</WebSite>";
                            paragraph = paragraph.Replace(emailText, hrefText);
                        }

                        i = paragraph.IndexOf("<email>");
                    }
                }

                i = paragraph.IndexOf("<uri");
                while (i >= 0)
                {
                    int j = paragraph.IndexOf("</uri>");
                    if (j < i)
                    {
                        if (j >= 0)
                        {
                            paragraph = paragraph.Substring(0, j) + paragraph.Substring(j + 6);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        string xrefText = paragraph.Substring(i, j - i + 6);

                        if (paragraph.Contains(xrefText))
                        {
                            var previousChar = char.MinValue;

                            var xrefTextIndex = paragraph.IndexOf(xrefText);
                            if (xrefTextIndex > 0)
                            {
                                previousChar = paragraph[xrefTextIndex - 1];
                            }
                            var nextChar = "";
                            if (xrefTextIndex + xrefText.Length < paragraph.Length - 1)
                            {
                                nextChar = paragraph[xrefTextIndex + xrefText.Length].ToString();
                            }

                            string hrefText = "";
                            XmlDocument xml = new XmlDocument();
                            xml.LoadXml(xrefText);
                            XmlNode node = xml.SelectSingleNode("uri");
                            if (node != null && node.Attributes["xlink:href"] != null)
                            {
                                if (!breakUrl && ((node.InnerText.Contains("/") || node.InnerText.Contains(" ") || node.InnerText.Contains("."))
                                    && (previousChar == '(' || previousChar == '“' || previousChar == '"' || nextChar == ")")
                                    && !node.InnerText.Contains("creativecommons.org/licenses/by/4.0")))
                                {
                                    if (xrefTextIndex + xrefText.Length < paragraph.Length - 1)
                                    {
                                        if (paragraph[xrefTextIndex + xrefText.Length] == ')' 
                                            || paragraph[xrefTextIndex + xrefText.Length] == '”'
                                            || paragraph[xrefTextIndex + xrefText.Length] == '"')
                                        {
                                            nextChar = paragraph[xrefTextIndex + xrefText.Length].ToString();
                                            if (paragraph[xrefTextIndex + xrefText.Length + 1] == '.' 
                                                || paragraph[xrefTextIndex + xrefText.Length + 1] == ','
                                                || paragraph[xrefTextIndex + xrefText.Length + 1] == ';')
                                            {
                                                nextChar += paragraph[xrefTextIndex + xrefText.Length + 1];
                                            }
                                        }
                                    }
                                    xrefText = previousChar + xrefText + nextChar;
                                    if (node.InnerText.Contains("/"))
                                    {
                                        var nodeTextFirstPart = string.Empty;
                                        var nodeTextLastPart = string.Empty;

                                        if (node.InnerText.Contains("//"))
                                        {
                                            nodeTextFirstPart = node.InnerText.Substring(0, node.InnerText.IndexOf("//") + 2);
                                            nodeTextLastPart = node.InnerText.Substring(node.InnerText.IndexOf("//") + 2);
                                        }
                                        else
                                        {
                                            nodeTextFirstPart = node.InnerText.Substring(0, node.InnerText.IndexOf("/") + 1);
                                            nodeTextLastPart = node.InnerText.Substring(node.InnerText.IndexOf("/") + 1);
                                        }

                                        if (previousChar == ' ')
                                        {
                                            xrefText = xrefText.TrimStart();
                                            hrefText = "<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + nodeTextFirstPart + "</WebSite></InlineParagraph>";
                                        }
                                        else
                                        {
                                            hrefText = "<InlineParagraph>" + previousChar + "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + nodeTextFirstPart + "</WebSite></InlineParagraph>";
                                        }
                                        if (!string.IsNullOrEmpty(nodeTextLastPart))
                                        {
                                            var lastChar = "";
                                            if (nodeTextLastPart.EndsWith("/"))
                                            {
                                                lastChar = "/";
                                                nodeTextLastPart = nodeTextLastPart.Substring(0, nodeTextLastPart.Length - 1);
                                            }
                                            if (nodeTextLastPart.Contains("/"))
                                            {
                                                var lastFirstPart = nodeTextLastPart.Substring(0, nodeTextLastPart.LastIndexOf("/") + 1);
                                                var lastLastPart = nodeTextLastPart.Substring(nodeTextLastPart.LastIndexOf("/") + 1);

                                                hrefText += "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + lastFirstPart + "</WebSite>";

                                                var lastLastPartArray = lastLastPart.Split(new char[] { '-' });

                                                var thisSplitChar = '-';

                                                var lastIndex1 = lastLastPart.LastIndexOf("-");
                                                var lastIndex2 = lastLastPart.LastIndexOf("=");
                                                var lastIndex3 = lastLastPart.LastIndexOf("&");
                                                var lastIndex4 = lastLastPart.LastIndexOf(".");
                                                var lastIndex5 = lastLastPart.LastIndexOf("+");
                                                var lastIndex6 = lastLastPart.LastIndexOf("%");
                                                var lastIndex = 0;
                                                if (lastIndex1 > lastIndex)
                                                {
                                                    lastIndex = lastIndex1;
                                                }
                                                if (lastIndex2 > lastIndex)
                                                {
                                                    lastIndex = lastIndex2;
                                                    thisSplitChar = '=';
                                                }
                                                if (lastIndex3 > lastIndex)
                                                {
                                                    lastIndex = lastIndex3;
                                                    thisSplitChar = '&';
                                                }
                                                if (lastIndex4 > lastIndex)
                                                {
                                                    lastIndex = lastIndex4;
                                                    thisSplitChar = '.';
                                                }
                                                if (lastIndex5 > lastIndex)
                                                {
                                                    lastIndex = lastIndex5;
                                                    thisSplitChar = '+';
                                                }
                                                if (lastIndex6 > lastIndex)
                                                {
                                                    lastIndex = lastIndex6;
                                                    thisSplitChar = '%';
                                                }
                                                if (lastIndex > 0)
                                                {
                                                    hrefText += "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + lastLastPart.Substring(0, lastIndex + 1) + "</WebSite>";
                                                    hrefText += "<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + lastLastPart.Substring(lastIndex + 1) + lastChar + "</WebSite>" + nextChar + "</InlineParagraph>";
                                                }
                                                else
                                                {
                                                    for (var il = 0; il < lastLastPartArray.Length; il++)
                                                    {
                                                        if (il == lastLastPartArray.Length - 1)
                                                        {
                                                            hrefText += "<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + lastLastPartArray[il] + lastChar + "</WebSite>" + nextChar + "</InlineParagraph>";
                                                        }
                                                        else
                                                        {
                                                            hrefText += "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + lastLastPartArray[il] + "-" + "</WebSite>";
                                                        }
                                                    }
                                                }



                                            }
                                            else if (nodeTextLastPart.Contains("."))
                                            {
                                                var linkTextArray = nodeTextLastPart.Split(new char[] { '.' });
                                                for (var il = 0; il < linkTextArray.Length; il++)
                                                {
                                                    if (il == linkTextArray.Length - 1)
                                                    {
                                                        hrefText += "<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + linkTextArray[il] + lastChar + "</WebSite>" + nextChar + "</InlineParagraph>";
                                                    }
                                                    else
                                                    {
                                                        hrefText += "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + linkTextArray[il] + "." + "</WebSite>";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                hrefText += "<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + nodeTextLastPart + lastChar + "</WebSite>" + nextChar + "</InlineParagraph>";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var splitChar = ' ';
                                        var textArray = node.InnerXml.Split(new char[] { splitChar });
                                        if (textArray.Length == 1)
                                        {
                                            splitChar = '.';
                                            textArray = node.InnerXml.Split(new char[] { splitChar });
                                        }
                                        var hrefTextBuilder = new StringBuilder();

                                        for (var tIndex = 0; tIndex < textArray.Length; tIndex++)
                                        {
                                            if (tIndex == 0)
                                            {
                                                hrefTextBuilder.Append("<InlineParagraph>" + previousChar + "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + textArray[tIndex] + splitChar + "</WebSite></InlineParagraph>");
                                            }
                                            else if (tIndex == textArray.Length - 1)
                                            {

                                                hrefTextBuilder.Append("<InlineParagraph><WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + textArray[tIndex] + "</WebSite>" + nextChar + "</InlineParagraph>");
                                            }
                                            else 
                                            {
                                                hrefTextBuilder.Append("<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + textArray[tIndex] + splitChar + "</WebSite>");
                                            }
                                        }

                                        hrefText = hrefTextBuilder.ToString();
                                    }

                                    paragraph = paragraph.Replace(xrefText, hrefText);
                                }
                                else
                                {
                                    hrefText = "<WebSite href='" + node.Attributes["xlink:href"].InnerText + "'>" + node.InnerText + "</WebSite>";
                                    paragraph = paragraph.Replace(xrefText, hrefText);
                                }
                            }
                        }
                        i = paragraph.IndexOf("<uri");
                    }
                }
                paragraph = paragraph.Replace("<sup>&reg;</sup>", "&reg;");
                paragraph = paragraph.Replace("&reg;", "<sup>&reg;</sup>");

                


                paragraph = paragraph.Replace("<bold>", "<Bold>");
                paragraph = paragraph.Replace("</bold>", "</Bold>");
                paragraph = paragraph.Replace("<italic>", "<Italic>");
                paragraph = paragraph.Replace("</italic>", "</Italic>");
                paragraph = paragraph.Replace("<strike>", "<Strike>");
                paragraph = paragraph.Replace("</strike>", "</Strike>");
                paragraph = paragraph.Replace("<underline>", "<Underline>");
                paragraph = paragraph.Replace("</underline>", "</Underline>");
                paragraph = paragraph.Replace("<i>", "<Italic>");
                paragraph = paragraph.Replace("</i>", "</Italic>");
                paragraph = paragraph.Replace("<sup>", "<Superscript>");
                paragraph = paragraph.Replace("</sup>", "</Superscript>");
                paragraph = paragraph.Replace("<sub>", "<Subscript>");
                paragraph = paragraph.Replace("</sub>", "</Subscript>");

                paragraph = paragraph.Replace("<styled-content", "<StyledContent");
                paragraph = paragraph.Replace("</styled-content>", "</StyledContent>");

                paragraph = paragraph.Replace(" </Italic>", "</Italic> ");

               

                if (isTable)
                {
                    var innerXml = "<Table>" + paragraph + "</Table>";
                    XmlDocument contentDoc = new XmlDocument();
                    contentDoc.LoadXml(innerXml);

                    XmlNodeList headerRowList = contentDoc.ChildNodes[0].SelectNodes("./thead/tr/th");
                    foreach (XmlNode node in headerRowList)
                    {
                        InitObjectReferenceForCell(node, referenceIdPrefix);
                    }

                    headerRowList = contentDoc.ChildNodes[0].SelectNodes("./thead/tr/td");
                    foreach (XmlNode node in headerRowList)
                    {
                        InitObjectReferenceForCell(node, referenceIdPrefix);
                    }

                    XmlNodeList bodyRowList = contentDoc.ChildNodes[0].SelectNodes("./tbody/tr/td");
                    foreach (XmlNode node in bodyRowList)
                    {
                        InitObjectReferenceForCell(node, referenceIdPrefix);
                    }

                    paragraph = contentDoc.ChildNodes[0].InnerXml;
                }
                else
                {
                    paragraph = InitBibrReference(paragraph, "[<xref", "</xref>]", !isTableCell, true, referenceIdPrefix);
                    paragraph = InitObjectReference(paragraph, "<xref", "</xref>", "", "", true, !isTableCell, referenceIdPrefix);



                }
                
                
                if (processEndingWord)
                {
                    //paragraph = InitParagrahEndingWords(paragraph);
                }

                paragraph = ReplaceHtmlEscape(paragraph);

                paragraph = TextHelper.InitXml(paragraph);
                paragraph = HandleWordsTag(paragraph);

                paragraph = paragraph.Replace("<Italic></Italic>", "");
                paragraph = paragraph.Replace("<Subscript></Subscript>", "");
                paragraph = paragraph.Replace("<Superscript></Superscript>", "");
                paragraph = paragraph.Replace(" </Italic>", "</Italic> ");
                paragraph = InitParagraphManualAddLineBreak(paragraph);
                paragraph = SpecialCharHelper.SetFont(paragraph);

                if (!isTable && paragraph.Contains("Font>"))
                {
                    var textArray = paragraph.Split(new char[] { ' ' });
                    foreach (var t in textArray)
                    {
                        if (t.Contains("Font>"))
                        {
                            var subTArray = t.Split(new char[] { '-' });
                            foreach (var subT in subTArray)
                            {
                                if (subT.Contains("Font>")
                                    && !subT.StartsWith("<")
                                    && !subT.Contains("<InlineParagraph>")
                                    && !subT.Contains("<Anchor")
                                    && !subT.Contains("</Italic>")
                                    && !subT.Contains("</Bold>"))
                                {
                                    paragraph = paragraph.Replace(" " + subT + " ", " <InlineParagraph>" + subT + "</InlineParagraph> ");
                                    paragraph = paragraph.Replace(subT + " ", "<InlineParagraph>" + subT + "</InlineParagraph> ");
                                    paragraph = paragraph.Replace(" " + subT, " <InlineParagraph>" + subT + "</InlineParagraph>");
                                }
                            }
                        }
                    }
                }


                paragraph = paragraph.Replace(" </InlineParagraph>", "</InlineParagraph> ");
                paragraph = paragraph.Replace("> <", "><Space/><");

            }
            return paragraph;
        }

        

        private static string InitParagraphManualAddLineBreak(string paragraph)
        {
            var startFlag = "[***]";
            var endFlag = "[/***]";
            paragraph = paragraph.Replace("<InlineParagraph>" + startFlag, startFlag);
            paragraph = paragraph.Replace(endFlag + "</InlineParagraph>", endFlag);
            if (paragraph.Contains(startFlag))
            {
                var index = paragraph.IndexOf(startFlag);
                while (index > 0)
                {
                    var endIndex = paragraph.IndexOf(endFlag);
                    var replacingText = paragraph.Substring(index, endIndex - index + endFlag.Length);

                    var replacedText = replacingText.Replace("<InlineParagraph>", "").Replace("</InlineParagraph>", "");
                    replacedText = replacedText.Replace(startFlag, "<InlineParagraph>").Replace(endFlag, "</InlineParagraph>");
                    paragraph = paragraph.Replace(replacingText, replacedText);
                    index = paragraph.IndexOf(startFlag);
                }
            }

            return paragraph;
        }

        
        private static string HandleHyphen(string paragraph)
        {
#if DEBUG
            if (paragraph.Contains("Assessment of related anamnestic and clinical factors"))
            {

            }
#endif
            paragraph = paragraph.Replace(" </italic>", "</italic> ");
            var array = paragraph.Split(new char[] { ' ' });
            foreach (var t in array)
            {
                if (t.Contains("-"))
                {
                    var ta = t.Split(new char[] { '-' });
                    if (ta.Length == 2)
                    {
                        if (ta[0].Length <= 4 && !t.Contains("<italic>"))
                        {
                            paragraph = paragraph.Replace(" " + t, "<Space/><InlineParagraph>" + t + "</InlineParagraph>");
                            paragraph = paragraph.Replace(t + " ", "<InlineParagraph>" + t + "</InlineParagraph><Space/>");
                        }
                        else if (ta[0].StartsWith("<") && ta[0].EndsWith("/>"))
                        {
                            var innerText = GetXmlInnerText(ta[0]);
                            if (innerText.Length <= 4)
                            {
                                paragraph = paragraph.Replace(" " + t, "<Space/><InlineParagraph>" + t + "</InlineParagraph>");
                                paragraph = paragraph.Replace(t + " ", "<InlineParagraph>" + t + "</InlineParagraph><Space/>");
                            }
                        }
                    }
                }
            }
            return paragraph;
        }
        private static string InitParagraphLineBreak(string paraXml, XmlNode rootNode, List<string> handledTexts)
        {
            return paraXml;
        }

        private static void InitObjectReferenceForCell(XmlNode node, string referenceIdPrefix)
        {
            var innerXml = node.InnerXml;
            innerXml = InitBibrReference(innerXml, "[<xref", "</xref>]", true, false, referenceIdPrefix);
            innerXml = InitObjectReference(innerXml, "<xref", "</xref>", "", "", true, true, referenceIdPrefix, false);
            node.InnerXml = innerXml;

        }
        private static string InitObjectReference(string paragraph, string matchBegin, string matchEnd,
            string prefixCharacter, 
            string suffixCharacter, 
            bool nextCharacterIncluded, 
            bool previousWordIncluded, 
            string referenceIdPrefix,
            bool handleJoinInlineTag = true)
        {

            //paragraph = paragraph.Replace("<Bold><xref", "<xref");
            //paragraph = paragraph.Replace("</xref></Bold>", "</xref>");
            int i = paragraph.IndexOf(matchBegin);
            while (i >= 0)
            {
                int j = paragraph.IndexOf(matchEnd, i);
                if (j < i)
                {
                    if (j >= 0)
                    {
                        paragraph = paragraph.Substring(0, j) + paragraph.Substring(j + matchEnd.Length);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    string xrefText = paragraph.Substring(i, j - i + matchEnd.Length);
                    string replacedXrefText = xrefText;

                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(xrefText);
                    XmlNode node = xml.SelectSingleNode("xref");
                    if (node != null)
                    {
                        string refType = node.Attributes["ref-type"].InnerText;
                        string rId = node.Attributes["rid"].InnerText;
                        if (refType == RefType.Bibliographic)
                        {
                            string anchorText = GetAnchorXml(RefType.Bibliographic, referenceIdPrefix + rId, node.InnerText);
                            paragraph = paragraph.Replace(xrefText, anchorText);
                        }
                        else
                        {
                            var replacedXrefTextWithTitle = replacedXrefText;
                            string innerText = node.InnerXml;

                            if (paragraph.Contains(replacedXrefTextWithTitle))
                            {
                                replacedXrefText = replacedXrefTextWithTitle;
                            }

                            bool isSup = false;
                            if (refType == "table-fn" || refType == "fn")
                            {
                                isSup = true;
                            }
                            string anchorText = GetAnchorXml(refType, referenceIdPrefix + rId, innerText, isSup);





                            int refIndex = paragraph.IndexOf(replacedXrefText);

                            


                            if (refIndex > 0 && previousWordIncluded)
                            {
                                if (paragraph[refIndex - 1] == '(' 
                                    || paragraph[refIndex - 1] == '[' 
                                    || paragraph[refIndex - 1] == '"'
                                    || paragraph[refIndex - 1] == '“'
                                    || paragraph[refIndex - 1] == '‘')
                                {
                                    replacedXrefText = paragraph[refIndex - 1] + replacedXrefText;
                                    anchorText = anchorText.Insert(0, paragraph[refIndex - 1].ToString());
                                }
                            }

                            if (nextCharacterIncluded && refIndex >= 0)
                            {
                                var followingStr = paragraph.Substring(paragraph.IndexOf(replacedXrefText) + replacedXrefText.Length);
                                List<string> characters = new List<string>() { ").", "),", ".", ",", ")", "]", "\"", "”.", "”", "’" };

                                foreach (var c in characters)
                                {
                                    if (followingStr.StartsWith(c))
                                    {
                                        replacedXrefText = replacedXrefText + c;
                                        anchorText = anchorText + c;
                                        break;
                                    }
                                }
                            }
                            if (refType == "sec")
                            {
                                anchorText = anchorText.Replace("</Anchor><Anchor", "</Anchor></InlineParagraph><InlineParagraph><Anchor");
                            }
                            if (replacedXrefText.Contains(")") && !replacedXrefText.StartsWith("(") && paragraph.Contains("(" + replacedXrefText))
                            {
                                paragraph = paragraph.Replace("(" + replacedXrefText, "<InlineParagraph>(" + anchorText + "</InlineParagraph>");
                            }
                            else
                            {
                                paragraph = Mavercloud.PDF.Helpers.String.ReplaceFirst(paragraph, replacedXrefText, "<InlineParagraph>" + anchorText + "</InlineParagraph>");
                                //paragraph = paragraph.Replace(replacedXrefText, "<InlineParagraph>" + anchorText + "</InlineParagraph>");
                            }

                        }
                    }
                    i = paragraph.IndexOf(matchBegin);
                }

            }
            if (handleJoinInlineTag)
            {
                //paragraph = paragraph.Replace("</InlineParagraph><InlineParagraph>", "");
            }
            return paragraph;
        }

        private static string InitBibrReference(string paragraph,
            string matchBegin, string matchEnd,
            bool previousWordIncluded, bool nextCharacterIncluded, 
            string referenceIdPrefix,
            string endStr = null)
        {

            int i = paragraph.IndexOf(matchBegin);
            while (i >= 0)
            {
                int j = paragraph.IndexOf(matchEnd, i);
                if (j < i)
                {
                    if (j >= 0)
                    {
                        paragraph = paragraph.Substring(0, j) + paragraph.Substring(j + matchEnd.Length);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(endStr))
                    {
                        var pureMatch = matchEnd.Substring(0, matchEnd.Length - endStr.Length);
                        if (paragraph.IndexOf(pureMatch, i) != j)
                        {
                            i = paragraph.IndexOf(matchBegin, i + 1);
                            continue;
                        }
                    }

                    string xrefText = paragraph.Substring(i, j - i + matchEnd.Length);

                    string replaceXrefText = xrefText;

                    StringBuilder supText = new StringBuilder();

                    var rootNode = TransferToNode(xrefText);

                    var textFlag = string.Empty;

                    var notBibr = false;
                    foreach (XmlNode textNode in rootNode.ChildNodes)
                    {
                        if (textNode.NodeType == XmlNodeType.Element)
                        {
                            if (textNode.Name.ToLower() == "xref")
                            {
                                string refType = textNode.Attributes["ref-type"].InnerText;
                                if (refType != "bibr")
                                {
                                    //textFlag = "XrefText";
                                    //previousWordIncluded = false;
                                    notBibr = true;
                                }
                                break;
                            }
                        }
                    }

                    if (notBibr)
                    {
                        i = paragraph.IndexOf(matchBegin, i + 1);
                        continue;
                    }


                    foreach (XmlNode textNode in rootNode.ChildNodes)
                    {
                        if (textNode.NodeType == XmlNodeType.Text)
                        {
                            if (string.IsNullOrEmpty(textFlag))
                            {
                                supText.Append(textNode.InnerText);
                            }
                            else
                            {
                                supText.Append(string.Format("<{0}>{1}</{0}>", textFlag, textNode.InnerText));
                            }
                        }
                        else if (textNode.NodeType == XmlNodeType.Element)
                        {
                            if (textNode.Name.ToLower() == "xref")
                            {
                                string refType = textNode.Attributes["ref-type"].InnerText;
                                string rId = textNode.Attributes["rid"].InnerText;
                                supText.Append(GetAnchorXml(refType, referenceIdPrefix + rId, textNode.InnerText, matchBegin.StartsWith("<sup>")));
                            }
                        }
                    }

                    int refIndex = paragraph.IndexOf(replaceXrefText);

                    if (refIndex > 0 && previousWordIncluded)
                    {
                        if (paragraph[refIndex - 1] == '(' || paragraph[refIndex - 1] == '[')
                        {
                            replaceXrefText = paragraph[refIndex - 1] + replaceXrefText;
                            supText = supText.Insert(0, paragraph[refIndex - 1]);
                        }
                    }

                    
                    var finalSupText = supText.ToString();
                    if (finalSupText.Contains("</Anchor>, <Anchor") || finalSupText.Contains("–"))
                    {
                        finalSupText = finalSupText.Replace("</Anchor>, <Anchor", "</Anchor>, </InlineParagraph><InlineParagraph><Anchor");
                        finalSupText = finalSupText.Replace("</Anchor>–<Anchor", "</Anchor>–</InlineParagraph><InlineParagraph><Anchor");
                    }
                    if (nextCharacterIncluded)
                    {
                        foreach (var character in nextIncludedcharacters)
                        {
                            var replacingText = string.Empty;
                            if (character != ",")
                            {
                                replacingText = "<InlineParagraph>" + finalSupText + character + " " + "</InlineParagraph>";
                                paragraph = paragraph.Replace(replaceXrefText + character + " ", replacingText);
                            }

                            replacingText = "<InlineParagraph>" + finalSupText + character + "</InlineParagraph>";
                            paragraph = paragraph.Replace(replaceXrefText + character, replacingText);
                        }
                    }

                    if (previousWordIncluded)
                    {
                        paragraph = paragraph.Replace(replaceXrefText, "<InlineParagraph>" + finalSupText + "</InlineParagraph>");
                    }
                    else
                    {
                        if (!finalSupText.Contains("<InlineParagraph>") && !finalSupText.StartsWith("<InlineParagraph>"))
                        {
                            finalSupText = "<InlineParagraph>" + finalSupText + "</InlineParagraph>";
                        }
                        else if (finalSupText.Contains("</InlineParagraph>") && !finalSupText.StartsWith("<InlineParagraph>"))
                        {
                            finalSupText = "<InlineParagraph>" + finalSupText + "</InlineParagraph>";
                        }
                        paragraph = paragraph.Replace(replaceXrefText, finalSupText);
                    }
                    i = paragraph.IndexOf(matchBegin, i + 1);
                }
            }
            return paragraph;
        }

#endregion

        public void Dispose()
        {
            if (pdfDocument != null)
            {
                try
                {
                    if (!pdfDocument.IsClosed())
                    {
                        pdfDocument.Close();
                        if (document != null)
                        {
                            document.Close();
                        }
                        if (pdfWriter != null)
                        {
                            pdfWriter.Dispose();
                        }
                    }
                }
                catch { }
            }
        }

        private class ArticleElement
        {
            //1 Title 0 Paragraph 2 Figure 3 Table 4 List
            public int Type { get; set; }

            public int TitleLevel { get; set; }

            public string Id { get; set; }

            public iText.Layout.Element.Paragraph Element { get; set; }

            public iText.Layout.Element.List ListElement { get; set; }

            public bool? FollowingParagraphIndent { get; set; }

            public bool? ExplicitNotParagraphIndent { get; set; }

            public bool? IsAbbreviationsTitle { get; set; }

            
        }


        public class BizHtmlArticleTableDivHandler : HtmlArticleTableDivHandler
        {
            private string referenceIdPrefix;
            private Dictionary<string, ParagraphStyleInfo> paragraphStyles;
            public BizHtmlArticleTableDivHandler(object sourceData,
                XmlNode tableXmlNode,
                ElementItem tableItem,
                string referenceIdPrefix,
                string styleName,
                iText.Layout.Document document,
                TableType? tableType,
                TableDisplayType? tableDisplayType, Dictionary<string, ParagraphStyleInfo> paragraphStyles)
                : base(sourceData, tableXmlNode, tableItem, null, styleName, document, tableType, tableDisplayType)
            {
                this.referenceIdPrefix = referenceIdPrefix;
                this.paragraphStyles = paragraphStyles;
            }

            public BizHtmlArticleTableDivHandler(object sourceData,
                XmlNode tableXmlNode,
                ElementItem tableItem,
                string referenceIdPrefix,
                BorderStyle headerCellBorder,
                string styleName,
                iText.Layout.Document document,
                TableType? tableType,
                TableDisplayType? tableDisplayType, Dictionary<string, ParagraphStyleInfo> paragraphStyles)
                : base(sourceData, tableXmlNode, tableItem, headerCellBorder, styleName, document, tableType, tableDisplayType)
            {
                this.referenceIdPrefix = referenceIdPrefix;
                this.paragraphStyles = paragraphStyles;
            }

            protected override List CreateTableCellList(string innerXml)
            {
                var sectionListItem = tableTableItem.Items.First(t => t.Name == "TableContentList");
                var jatsAnalyser = new JATSAnalyzer();
                var listWrapper = jatsAnalyser.GetEntity<ListWrapper>(innerXml);
                var listInfo = GetArticleJatsList(listWrapper, referenceIdPrefix);
                var jatsListHandler = new JatsListHandler(listInfo, sectionListItem, styleName, document);
                return jatsListHandler.Handle();
            }
        }


    }

    
}
