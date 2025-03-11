using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.Layout.Renderer;
using Mavercloud.PDF.General.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.Biz.Pages
{
    public class SplittingArticleParagraphRenderer : ParagraphRenderer
    {
        private SplittingArticleParagraphRenderer leftover;
        private SplittingArticleParagraphRenderer toDraw;

        private List<ArticleAddingTable> addingTables;
        private List<string> toDrawTableLinkIds;

        private float y;

        private int lastTableNumber = 0;
        private int lastFigureNumber = 0;
        private int lastSchemeNumber = 0;

        public SplittingArticleParagraphRenderer(Paragraph modelElement, List<ArticleAddingTable> addingTables) : base(modelElement)
        {
            this.addingTables = addingTables;
        }

        

        public override LayoutResult Layout(LayoutContext layoutContext)
        {
            var result = base.Layout(layoutContext);
            if (result.GetStatus() != LayoutResult.NOTHING)
            {
                y = result.GetOccupiedArea().GetBBox().GetBottom();
            }
            if (result.GetStatus() == LayoutResult.PARTIAL)
            {
                // Expected result here is that paragraph splits across pages
                leftover = (SplittingArticleParagraphRenderer)result.GetOverflowRenderer();
                toDraw = (SplittingArticleParagraphRenderer)result.GetSplitRenderer();
                if (toDraw.toDraw != null)
                {
                    toDraw.toDraw = null;
                }
                return new LayoutResult(LayoutResult.FULL, result.GetSplitRenderer().GetOccupiedArea(), null, null);
            }
            else
            {
                return result;
            }
        }

        public override void Draw(DrawContext drawContext)
        {
            if (toDraw != null)
            {
                if (leftover != null)
                {
                    toDraw.SetProperty(Property.PADDING_BOTTOM, 0f);
                }
                toDraw.Draw(drawContext);
            }
            else
            {
                base.Draw(drawContext);
            }
        }

        private void InitTableLinks(Link link)
        {
            var pdfObject = link.GetLinkAnnotation().GetAction().Get(PdfName.D);
            if (pdfObject != null && pdfObject.IsString())
            {
                var destination = (pdfObject as PdfString).GetValue();
                var destTable = addingTables.FirstOrDefault(t => t.Id == destination && t.Drawn == false);
                if (destTable != null)
                {
                    if (!toDrawTableLinkIds.Contains(destTable.Id))
                    {
                        if (destTable.Type == 0)
                        {
                            if (lastTableNumber == 0)
                            {
                                toDrawTableLinkIds.Add(destTable.Id);
                            }
                            else
                            {
                                for (int ti = lastTableNumber + 1; ti <= destTable.Number; ti++)
                                {
                                    if (addingTables.Any(t => t.Number == ti && t.Type == destTable.Type))
                                    {
                                        toDrawTableLinkIds.Add(addingTables.First(t => t.Number == ti && t.Type == destTable.Type).Id);
                                    }
                                }
                            }
                        }
                        else if (destTable.Type == 1)
                        {
                            if (lastFigureNumber == 0)
                            {
                                toDrawTableLinkIds.Add(destTable.Id);
                            }
                            else
                            {
                                for (int ti = lastFigureNumber + 1; ti <= destTable.Number; ti++)
                                {
                                    if (addingTables.Any(t => t.Number == ti && t.Type == destTable.Type))
                                    {
                                        toDrawTableLinkIds.Add(addingTables.First(t => t.Number == ti && t.Type == destTable.Type).Id);
                                    }
                                }
                            }
                        }
                        

                        if (destTable.Type == 0)
                        {
                            lastTableNumber = destTable.Number;
                        }
                        else if (destTable.Type == 1)
                        {
                            lastFigureNumber = destTable.Number;
                        }
                        else
                        {
                            lastSchemeNumber = destTable.Number;
                        }

                    }
                }
            }
        }

        public void InitTableLinks(IPropertyContainer element)
        {
            if (toDrawTableLinkIds == null)
            {
                toDrawTableLinkIds = new List<string>();
            }
            if (element is Link)
            {
                InitTableLinks(element as Link);
            }
            else if (element is Paragraph)
            {
                var para = element as Paragraph;
                foreach (var child in para.GetChildren())
                {
                    InitTableLinks(child);
                }
            }
        }

        public void InitTableLinks()
        {
            toDrawTableLinkIds = new List<string>();
            SplittingArticleParagraphRenderer renderer = toDraw == null ? this : toDraw;

            foreach (var childRenderer in renderer.GetChildRenderers().ToList())
            {
                InitTableLinks(childRenderer.GetModelElement());
            }
        }

        public override IRenderer GetNextRenderer()
        {
            return new SplittingArticleParagraphRenderer((Paragraph)modelElement, addingTables);
        }

        public float GetY()
        {
            return y;
        }

        public List<string> GetToDrawTableLinkIds()
        {
            return toDrawTableLinkIds;
        }

        public SplittingArticleParagraphRenderer GetLeftOver()
        {
            return leftover;
        }
    }
}
