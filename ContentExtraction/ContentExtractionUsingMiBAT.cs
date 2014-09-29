using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ContentExtraction
{
    class ContentExtractionUsingMiBAT
    {
        // Reference:
        //   Xinying Song, Jing Liu, Yunbo Cao, Chin-Yew Lin, Hsiao-Wuen Hon.
        //   Automatic extraction of web data records containing user-generated content.
        //   In Proceedings of the 19th ACM international conference on Information and knowledge management, pp.39–48, 2010.
        //   http://doi.acm.org/10.1145/1871437.1871447
        //
        //   Xinying Song, Zhiyuan Chen, Yunbo Cao, Chin-Yew Lin.
        //   Domain Constraint Path Based Data Record Extraction. 2012.
        //   http://www.google.com/patents/US20120124086

        // Domain Constraints (post-date)
        static readonly Regex RegexDomainConstraints = new Regex("(?:[12]+月[0-3]+日|[12]+[/-][0-3]+|[1-9]+時[0-9]+分|[1-9]+:[0-9]+)", RegexOptions.IgnoreCase);
        
        private HtmlElement _Node;
        private HashSet<HtmlElement> ContentNodeSet = new HashSet<HtmlElement>();

        public ContentExtractionUsingMiBAT(string urlString)
        {
            NonDispBrowser wb = new NonDispBrowser();
            wb.NavigateAndWait(urlString);

            HtmlDocument doc = wb.Document;
            //_Node = doc.Body;
            _Node = doc.GetElementsByTagName("html")[0];

            foreach (HtmlElement node in _Node.All)
            {
                HashSet<HtmlElement> contentNodeSet = MiBAT(node);

                foreach (HtmlElement contentNode in contentNodeSet)
                {
                    ContentNodeSet.Add(contentNode);
                }
            }
        }

        public HtmlElement Node
        {
            get { return _Node; }
        }
        public List<HtmlElement> Content
        {
            get { return ContentNodeSet.ToList(); }
        }
        public List<HtmlElement> Comment
        {
            get { return ContentNodeSet.ToList(); }
        }

        HashSet<HtmlElement> MiBAT(HtmlElement node)
        {
            List<List<int>> anchorTrees = FindAnchorTrees(node.Children);

            HashSet<HtmlElement> contentNodeSet = new HashSet<HtmlElement>();
            foreach (List<int> tree in anchorTrees)
            {
                List<List<HtmlElement>> R = DetermineBoundary(node.Children, tree);
                foreach (List<HtmlElement> Rx in R)
                {
                    foreach (HtmlElement contentNode in Rx)
                    {
                        contentNodeSet.Add(contentNode);
                    }
                }
            }

            return contentNodeSet;
        }

        List<List<int>> FindAnchorTrees(HtmlElementCollection nodes)
        {
            //List<HtmlElement> trees = new List<HtmlElement>();
            List<List<int>> trees = new List<List<int>>();

            int[] covered = Enumerable.Repeat(0, nodes.Count).ToArray();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (covered[i] == 1)
                {
                    continue;
                }

                List<int> a = new List<int>();
                a.Add(i);
                int m = 0;

                List<List<string>> CPSet = CandidatePivots(nodes[i]);
                if (CPSet.Count == 0)
                {
                    continue;
                }

                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (covered[j] == 1)
                    {
                        continue;
                    }

                    List<List<string>> matchedCP = DomainCompare(nodes[i], nodes[j], CPSet);

                    if (matchedCP.Count > 0)
                    {
                        m = m + 1;
                        a.Add(j);
                        CPSet = matchedCP;
                        covered[j] = 1;
                    }
                }

                if (m >= 2)
                {
                    trees.Add(a);
                }
            }

            return trees;
        }

        List<List<string>> CandidatePivots(HtmlElement t_i)
        {
            List<List<string>> pivots = new List<List<string>>();

            foreach (HtmlElement node in t_i.All)
            {
                if (node.Children.Count > 0)
                {
                    continue;
                }

                if (node.InnerText == null)
                {
                    continue;
                }

                int formatCount = RegexDomainConstraints.Matches(node.InnerText).Count;

                if (formatCount > 0)
                {
                    List<string> pivot = new List<string>();

                    HtmlElement elm = node;
                    while (elm != null && elm != t_i)
                    {
                        pivot.Add(elm.TagName);

                        elm = elm.Parent;
                    }

                    pivots.Add(pivot);
                }
            }

            return pivots;
        }

        // CPSet = CandidatePivots(t_i);
        List<List<string>> DomainCompare(HtmlElement t_i, HtmlElement t_j, List<List<string>> CPSet)
        {
            //int M = SimpleTreeMatching(t_i, t_j);

            List<List<string>> matchedCP = new List<List<string>>();

            List<List<string>> vSet = CandidatePivots(t_j);

            HashSet<string> CPTags = new HashSet<string>();
            foreach (List<string> u in CPSet)
            {
                CPTags.Add(string.Join(" ", u));
            }

            foreach (List<string> v in vSet)
            {
                if (CPTags.Contains(string.Join(" ", v)))
                {
                    matchedCP.Add(v);
                }
            }

            return matchedCP;
        }

        List<List<HtmlElement>> DetermineBoundary(HtmlElementCollection nodes, List<int> a)
        {
            int anchorGap = a[1] - a[0];
            for (int i = 1; i < a.Count; i++)
            {
                if (anchorGap > a[i] - a[i - 1])
                {
                    anchorGap = a[i] - a[i - 1];
                }
            }

            int left = 0;
            for (int j = 1; j < Math.Min(anchorGap, a[0]); j++)
            {
                HashSet<string> tagSet = new HashSet<string>();
                for (int i = a[0] - j; i < a.Last() - j; i++)
                {
                    tagSet.Add(nodes[i].TagName);
                }

                if (tagSet.Count == 1)
                {
                    left = left - 1;
                }
                else
                {
                    break;
                }
            }

            int right = 0;
            for (int j = 1; j < Math.Min(anchorGap - 1, nodes.Count - a.Last()); j++)
            {
                HashSet<string> tagSet = new HashSet<string>();
                for (int i = a[0] + j; i < a.Last() + j; i++)
                {
                    tagSet.Add(nodes[i].TagName);
                }

                if (tagSet.Count == 1)
                {
                    right = right + 1;
                }
                else
                {
                    break;
                }
            }

            int expanLen = right - left + 1;
            int k = Math.Min(anchorGap, expanLen);
            List<List<HtmlElement>> R = new List<List<HtmlElement>>();
            double score = 0;
            for (int x = k - expanLen; x <= 0; x++)
            {
                List<List<HtmlElement>> Rx = new List<List<HtmlElement>>();
                for (int i = 0; i < a.Count(); i++)
                {
                    List<HtmlElement> Ri = new List<HtmlElement>();
                    for (int j = 0; j <= k - 1; j++)
                    {
                        Ri.Add(nodes[a[i] + x + j]);
                    }
                    Rx.Add(Ri);
                }
                double _score = Score(Rx);
                if (score <= _score)
                {
                    R = Rx;
                    score = _score;
                }
            }

            return R;
        }

        double Score(List<List<HtmlElement>> Rx)
        {
            double score = 0;
            foreach (List<HtmlElement> Ri in Rx)
            {
                for (int i = 1; i < Ri.Count; i++)
                {
                    score += 2.0 * SimpleTreeMatching(Ri[i], Ri[i - 1]) / (Ri[i].All.Count + Ri[i - 1].All.Count);
                }
            }

            return score;
        }

        int SimpleTreeMatching(HtmlElement A, HtmlElement B)
        {
            if (A.TagName != B.TagName)
            {
                return 0;
            }

            int m = A.Children.Count;
            int n = B.Children.Count;

            int[,] M = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++)
            {
                M[i, 0] = 0;
            }
            for (int j = 0; j <= n; j++)
            {
                M[0, j] = 0;
            }

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    M[i, j] = Math.Max(M[i, j - 1], Math.Max(M[i - 1, j], M[i - 1, j - 1] + SimpleTreeMatching(A.Children[i - 1], B.Children[j - 1])));
                }
            }

            return M[m, n] + 1;
        }
    }
}
