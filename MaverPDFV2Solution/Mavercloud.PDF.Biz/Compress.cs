using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Mavercloud.PDF.Biz
{
    public static class Compress
    {
        public static List<string> Uncompress(string filePath, string outputDir, bool extractFullPath = true)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                return Uncompress(stream, outputDir, extractFullPath);
            }
        }

        public static List<string> Uncompress(Stream fileStream, string outputDir, bool extractFullPath = true)
        {
            if (fileStream.CanSeek)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
            }
            using (var reader = ReaderFactory.Open(fileStream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(outputDir, new ExtractionOptions()
                        {
                            ExtractFullPath = extractFullPath,
                            Overwrite = true
                        });
                    }
                }
            }
            return Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories).ToList();
        }
    }
}
