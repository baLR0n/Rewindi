using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Rewindi.Model.Parsing;

namespace Rewindi.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args[0];

            GameInfoParser parser = new GameInfoParser();
            HtmlDocument doc = parser.GetParseDocument(path);

            // Get tables and nodes and etc.
            List<HtmlNode> tables = doc.DocumentNode.Descendants("table").ToList();
        }
    }
}
