using iText.Kernel.Pdf;
using iText.Layout.Element;
using Mavercloud.PDF.General.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mavercloud.PDF.General.Helpers
{
    public class ListTableLinkHelper
    {
        private int lastTableNumber = 0;
        private int lastFigureNumber = 0;
        private int lastSchemeNumber = 0;

        private List<ArticleAddingTable> addingTables;
        private List<string> toDrawTableLinkIds;
        private List list;


        public ListTableLinkHelper(List list, List<ArticleAddingTable> addingTables)
        {
            this.list = list;
            this.addingTables = addingTables;
            this.toDrawTableLinkIds = new List<string>();
        }
        public List<string> GetTableLinks()
        {
            InitTableLinks(list);
            return toDrawTableLinkIds;
        }

        private void InitTableLinks(List list)
        {
            var items = list.GetChildren();
            foreach (var item in items)
            {
                InitTableLinks(item as ListItem);
            }
        }

        private void InitTableLinks(Paragraph p)
        {
            var children = p.GetChildren();
            foreach (var child in children)
            {
                if (child is Link)
                {
                    var link = (Link)child;
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
                else if (child is Paragraph)
                {
                    InitTableLinks(child as Paragraph);
                }
            }
        }

        private void InitTableLinks(ListItem item)
        {
            var children = item.GetChildren();
            foreach (var child in children) 
            {
                if (child is List)
                {
                    InitTableLinks(child as List);
                }
                else if (child is Paragraph)
                {
                    InitTableLinks(child as Paragraph);
                }
            }
        }

    }
}
