using System.IO;
using HtmlAgilityPack;

namespace Rewindi.Model.Parsing
{
    public class GameInfoParser
    {
        public HtmlDocument GetParseDocument(string path)
        {
            if (File.Exists(path))
            {
                StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open));
                HtmlDocument doc = new HtmlDocument();

                doc.LoadHtml(reader.ReadToEnd());
                return doc;
            }

            return null;
        }
    }
}
