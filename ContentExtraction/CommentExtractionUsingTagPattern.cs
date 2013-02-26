using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ContentExtraction
{
    class CommentExtractionUsingTagPattern
    {
        // This class is not completed!!

        // Reference:
        //   Huan-An Kao, Hsin-Hsi Chen.
        //   Comment Extraction from Blog Posts and Its Applications to Opinion Mining.
        //   In Proceedings of the 7th International Conference on Language Resources and Evaluation, pp.1113-1120, 2010.
        //   http://www.lrec-conf.org/proceedings/lrec2010/pdf/17_Paper.pdf
        
        // Table 1 (pp.1114)
        static readonly Dictionary<string, int> CodingSchemeTag = new Dictionary<string, int>() { { "STRUCTURE", 1 }, { "DIV", 2 }, { "DT", 5 }, { "LI", 6 }, { "DD", 7 }, { "TR", 8 } };
        static readonly Dictionary<string, int> CodingSchemeAttribute = new Dictionary<string, int>() { { "CLASS", 3 }, { "ID", 4 } };

        static readonly Regex RegexComment = new Regex("(?:comment|コメント)", RegexOptions.IgnoreCase);
        static readonly Regex RegexPunctuation = new Regex(@"\{p}", RegexOptions.IgnoreCase);
        static readonly Regex RegexDateTime = new Regex("(?:[12]+月[0-3]+日|[12]+[/-][0-3]+|[1-9]+時[0-9]+分|[1-9]+:[0-9]+)", RegexOptions.IgnoreCase);
        
        private HtmlElement _Node;
        private Dictionary<string, int> _RuleCount = new Dictionary<string, int>();
        private Dictionary<string, List<Tuple<HtmlElement, int>>> _RuleNode = new Dictionary<string, List<Tuple<HtmlElement, int>>>();

        public CommentExtractionUsingTagPattern(string urlString)
        {
            NonDispBrowser wb = new NonDispBrowser();
            wb.NavigateAndWait(urlString);

            HtmlDocument doc = wb.Document;
            //_Node = doc.Body;
            _Node = doc.GetElementsByTagName("html")[0];

            CreateRule(_Node);

            foreach (KeyValuePair<string, int> kvp in _RuleCount.OrderByDescending(n => n.Value))
            {
                if (kvp.Value <= 1)
                {
                    break;
                }

                Console.WriteLine(string.Join("\t", kvp.Value, kvp.Key));
                foreach (Tuple<HtmlElement, int> nodes in _RuleNode[kvp.Key])
                {
                    Console.Write(string.Format("{0}:{1} ", nodes.Item1.TagName, nodes.Item2));
                }
                Console.WriteLine();
            }
        }

        // 2.2 Repetitive Pattern Identification (pp.1113-1114)
        List<int> CreateRule(HtmlElement node, int d = -1)
        {
            if (node == null)
            {
                return new List<int>();
            }

            List<int> codes = new List<int>();
            if (CodingSchemeTag.ContainsKey(node.TagName))
            {
                codes.Add(CodingSchemeTag[node.TagName]);

                foreach (string attributeName in CodingSchemeAttribute.Keys.OrderBy(n => n))
                {
                    if (!node.GetAttribute(attributeName).Equals(""))
                    {
                        codes.Add(CodingSchemeAttribute[attributeName]);
                    }
                }
            }
            else
            {
                codes.Add(0);
            }

            List<string> childrenRules = new List<string>();
            foreach (HtmlElement e in node.Children)
            {
                List<int> tmp = CreateRule(e, d + 1);

                foreach (int code in tmp)
                {
                    codes.Add(code);
                }

                childrenRules.Add(string.Join(",", tmp));
            }

            if (childrenRules.Count >= 2)
            {
                for (int i = 0; i < childrenRules.Count - 1; i++)
                {
                    string rule = childrenRules[i];
                    for (int j = i + 1; j < childrenRules.Count; j++)
                    {
                        rule += "," + childrenRules[j];

                        AddRule(rule, node.Children[i], j - i);
                    }
                }
            }

            if (CodingSchemeTag.ContainsKey(node.TagName))
            {
                codes.Add(-1 * CodingSchemeTag[node.TagName]);
            }

            AddRule(string.Join(",", codes), node, 0);

            //for (int i = 0; i < d; i++)
            //    Console.Write(" ");
            //Console.WriteLine(string.Format("{0} {1}", node.TagName, string.Join(",", codes)));

            return codes;
        }

        void CreateFeature(List<HtmlElement> nodes)
        {
            Dictionary<int, double> feature = new Dictionary<int,double>();
            foreach (HtmlElement node in nodes) {
                feature[1] = (feature.ContainsKey(1)) ? feature[1] + node.InnerText.Length : node.InnerText.Length;
                feature[2] = (feature.ContainsKey(2)) ? feature[2] + node.All.Count : node.All.Count;

                int val_4 = RegexComment.Matches(node.InnerText).Count;
                feature[4] = (feature.ContainsKey(4)) ? feature[4] + val_4 : val_4;

                int val_6 = 0;
                foreach (HtmlElement e in node.All)
                {
                    if (e.TagName.Equals("A"))
                    {
                        val_6++;
                    }
                }
                feature[6] = (feature.ContainsKey(6)) ? feature[6] + val_6 : val_6;

                int val_10 = RegexPunctuation.Matches(node.InnerText).Count;
                feature[10] = (feature.ContainsKey(10)) ? feature[10] + val_10 : val_10;

                int val_13 = RegexDateTime.Matches(node.InnerText).Count;
                feature[13] = (feature.ContainsKey(13)) ? feature[13] + val_13 : val_13;
            }

            feature[3] = feature[1] + feature[2];
            feature[5] = feature[6] / feature[2];
            
            feature[7] = 0; // ToDo : Ratio of stop words
            feature[8] = 0; // ToDo : Number of stop words

            feature[9] = feature[10] / feature[1];

            feature[11] = 0; // ToDo : Block start position
            feature[12] = 0; // ToDo : Block end position

            feature[14] = (feature[13] > 0) ? 1 : 0;

            feature[15] = 0; // ToDo : Rule start position
            feature[16] = 0; // ToDo : Rule end position
            feature[17] = 0; // ToDo : Density
            feature[18] = 0; // ToDo : Coverage
            feature[19] = 0; // ToDo : Regularity
        }

        void AddRule(string rule, HtmlElement node, int nextSiblingCount)
        {
            if (Regex.IsMatch(rule, @"^[0,]+$"))
            {
                return;
            }

            Tuple<HtmlElement, int> nodes = new Tuple<HtmlElement, int>(node, nextSiblingCount);
            if (_RuleCount.ContainsKey(rule))
            {
                _RuleCount[rule]++;
                _RuleNode[rule].Add(nodes);
            }
            else
            {
                _RuleCount[rule] = 1;
                _RuleNode[rule] = new List<Tuple<HtmlElement, int>>() { nodes };
            }
        }
    }
}
