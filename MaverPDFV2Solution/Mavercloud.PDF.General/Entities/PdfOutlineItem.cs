using System;
using System.Collections.Generic;
using System.Text;

namespace Mavercloud.PDF.General.Entities
{
    public class PdfOutlineItem
    {
        public string Id { get; set; }
        public string Text { get; set; }

        public string Destination { get; set; }

        public List<PdfOutlineItem> Items { get; set; }

        public PdfOutlineItem AddItem(string id, string text, string destination)
        {
            if (Items == null)
            {
                Items = new List<PdfOutlineItem>();
            }
            var item = new PdfOutlineItem() { Id = id, Text = text, Destination = destination };
            Items.Add(item);
            return item;
        }

        public PdfOutlineItem AddItem(PdfOutlineItem item)
        {
            if (Items == null)
            {
                Items = new List<PdfOutlineItem>();
            }
            Items.Add(item);
            return item;
        }
    }
}
