using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Rewindi.Model.GameInfo;

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

        /// <summary>
        /// Parses the log.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public List<Move> ParseLog(string path)
        {
            HtmlDocument logDoc = this.GetParseDocument(path);

            if (logDoc == null)
            {
                return null;
            }

            List<Move> logList = new List<Move>();

            foreach (HtmlNode col in logDoc.DocumentNode.SelectNodes("//table[@class='data']//tbody//tr//td"))
            {
                // When contains "Move" -> Parentnode.InnerText hat alle Infos.
                if (col.InnerText.Contains("Move"))
                {
                    string moveText = col.ParentNode.InnerText.Replace('\r', ' ').Replace('\t', ' ');
                    List<string> parts = moveText.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                    // Get coordinates
                    Regex regex = new Regex(@"([0-9]+,[0-9]+)");
                    Match positionMatch = regex.Match(parts[2]);

                    // get possible special stone
                    regex = new Regex(@"[*]");
                    Match specialMatch = regex.Match(moveText);
                    string specialMsg = specialMatch.Success ? specialMatch.Value : null;

                    Move newMove = new Move()
                    {
                        PosX = Convert.ToInt32(positionMatch.Value.Split(',')[0]),
                        PosY = Convert.ToInt32(positionMatch.Value.Split(',')[1]),
                        TeamName = parts[1],
                        SpecialMessage = specialMsg,
                        CalculationTime = parts[3].Trim().Substring(1)
                    };

                    logList.Add(newMove);
                }
            }

            return logList;
        }
    }
}
