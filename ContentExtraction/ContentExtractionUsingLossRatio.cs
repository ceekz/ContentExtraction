using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ContentExtraction
{
    class ContentExtractionUsingLossRatio
    {
        // Reference:
        //   Donglin Cao, Xiangwen Liao, Hongbo Xu, and Shuo Bai.
        //   Blog post and comment extraction using information quantity of web format.
        //   In Information Retrieval Technology: 4th Asia Information Retrieval Symposium, pp.298-309, 2008.
        //   http://dx.doi.org/10.1007/978-3-540-68636-1_29

        /*
        [STAThread]
        static void _Main(string[] args)
        {
            foreach (string urlString in args)
            {
                // Content Extraction
                ContentExtractionUsingLossRatio ce = new ContentExtractionUsingLossRatio(urlString);

                // View Content HTML
                foreach (HtmlElement e in ce.Content)
                {
                    Console.WriteLine(e.OuterHtml);
                }

                // View Post HTML
                foreach (HtmlElement e in ce.Post)
                {
                    Console.WriteLine(e.OuterHtml);
                }

                // View Cmmment HTML
                foreach (HtmlElement e in ce.Comment)
                {
                    Console.WriteLine(e.OuterHtml);
                }
            }
        }
        */

        // Additional Aomponents (References):
        //   System.Drawing
        //   System.Windows.Forms

        static readonly string[] SkipTagName = { "!", "STYLE", "SCRIPT", "NOSCRIPT", "SELECT" }; // "!" is a comment element.
        static int ThresholdTextLength = 10; // line 3, pp.302

        struct NodeProperty
        {
            public int width;
            public int textLength;
            public int linkTextLength;

            // Eq.1, pp.301
            // W_e (WordNum) = textLength - linkTextLength = contentTextLength
            // W_a (WholeWordNum) = textLength
            // I_e = effectiveTextInformation

            public int contentTextLength
            {
                get { return textLength - linkTextLength; }
            }
            public double effectiveTextInformation
            {
                get { return ((double)contentTextLength / textLength) * contentTextLength; }
            }
        }

        private HtmlElement _Node;
        private HtmlElement _ContentNode;
        private List<HtmlElement> _PostNode = new List<HtmlElement>();
        private List<HtmlElement> _CommentNode = new List<HtmlElement>();
        private Dictionary<HtmlElement, NodeProperty> _NodeProperty = new Dictionary<HtmlElement, NodeProperty>();
        private List<Tuple<HtmlElement, double>> _LossRatio = new List<Tuple<HtmlElement, double>>();

        public ContentExtractionUsingLossRatio(string urlString, int thresholdTextLength = 0)
        {
            if (thresholdTextLength > 0)
            {
                ThresholdTextLength = thresholdTextLength;
            }

            NonDispBrowser wb = new NonDispBrowser();
            wb.NavigateAndWait(urlString);

            HtmlDocument doc = wb.Document;
            //_Node = doc.Body;
            _Node = doc.GetElementsByTagName("html")[0];

            CreateNodeProperty(_Node);

            CreateLossRatio(_Node);

            FindContentNode();

            FindSeparator();
        }

        public HtmlElement Node
        {
            get { return _Node; }
        }
        public List<HtmlElement> Content
        {
            get { return new List<HtmlElement>() { _ContentNode }; }
        }
        public List<HtmlElement> Post
        {
            get { return _PostNode; }
        }
        public List<HtmlElement> Comment
        {
            get { return _CommentNode; }
        }
        public List<Tuple<HtmlElement, double>> LossRatio
        {
            get { return _LossRatio; }
        }

        // Locating main text algorithm 2, 3 (pp.301)
        int[] CreateNodeProperty(HtmlElement node, int d = -1)
        {
            if (node == null)
            {
                return new int[] { 0, 0 };
            }

            int textLength = 0;
            try
            {
                textLength = node.InnerText.Length;
            }
            catch { }

            int linkTextLength = 0;
            if (node.TagName.Equals("A"))
            {
                linkTextLength += textLength;
            }

            int skipTextLength = 0;
            if (SkipTagName.Contains(node.TagName))
            {
                skipTextLength += textLength;
            }

            foreach (HtmlElement e in node.Children)
            {
                int[] tmp = CreateNodeProperty(e, d + 1);

                linkTextLength += tmp[0];
                skipTextLength += tmp[1];
            }

            NodeProperty nodeProperty = new NodeProperty();
            nodeProperty.width = node.OffsetRectangle.Width;
            nodeProperty.textLength = textLength - skipTextLength;
            nodeProperty.linkTextLength = linkTextLength;

            _NodeProperty[node] = nodeProperty;

            //for (int i = 0; i < d; i++)
            //    Console.Write(" ");
            //Console.WriteLine(string.Format("{0} {1} {2} {3}", node.TagName, textLength, linkTextLength, skipTextLength));

            return new int[] { linkTextLength, skipTextLength };
        }

        // Locating main text algorithm 4, 4.1, 4.2 (pp.301-302)
        HtmlElement CreateLossRatio(HtmlElement node)
        {
            if (node == null)
            {
                return null;
            }

            HtmlElement candidateNode = null;
            foreach (HtmlElement e in node.Children)
            {
                if (SkipTagName.Contains(e.TagName))
                {
                    continue;
                }
                if (_NodeProperty[e].contentTextLength <= ThresholdTextLength)
                {
                    continue;
                }
                // This appears only on the author's source code. (not on the paper)
                //if (_NodeProperty[e].textLength <= 50 || _NodeProperty[e].width < 200)
                //{
                //    continue;
                //}

                if (candidateNode == null)
                {
                    candidateNode = e;
                    continue;
                }
                if (_NodeProperty[e].width < _NodeProperty[candidateNode].width)
                {
                    continue;
                }

                if (_NodeProperty[e].width > _NodeProperty[candidateNode].width || _NodeProperty[e].effectiveTextInformation > _NodeProperty[candidateNode].effectiveTextInformation)
                {
                    candidateNode = e;
                }
                // This appears only on the author's source code. (not on the paper)
                //else if (_NodeProperty[candidateNode].contentTextLength < 100 && _NodeProperty[e].contentTextLength > _NodeProperty[candidateNode].contentTextLength)
                //{
                //    candidateNode = e;
                //}
            }

            if (candidateNode == null)
            {
                return null;
            }

            _LossRatio.Add(new Tuple<HtmlElement, double>(candidateNode, _NodeProperty[node].effectiveTextInformation / _NodeProperty[candidateNode].effectiveTextInformation));

            return CreateLossRatio(candidateNode);
        }

        // Locating main text algorithm 4.3 (pp.302)
        void FindContentNode()
        {
            _ContentNode = _LossRatio[0].Item1;

            foreach (Tuple<HtmlElement, double> lossRatio in _LossRatio)
            {
                if (lossRatio.Item2 > _LossRatio.Select(n => n.Item2).Average())
                {
                    break;
                }

                _ContentNode = lossRatio.Item1;
            }
        }

        // Finding separator algorithm (pp.304)
        void FindSeparator()
        {
            if (_ContentNode == null)
            {
                return;
            }

            HtmlElement node = _ContentNode;

            // This appears on on the author's source code, and is explained in closing paragraph of pp.304.
            while (node.Children.Count == 1)
            {
                node = node.Children[0];

                if (node == null)
                {
                    _PostNode.Add(_ContentNode);

                    return;
                }
            }

            // i(= nodeId) -> Tag -> Count
            Dictionary<int, Dictionary<string, int>> eTag = new Dictionary<int, Dictionary<string, int>>();
            for (int i = 0; i < node.Children.Count; i++)
            {
                eTag[i] = new Dictionary<string, int>();

                foreach (HtmlElement e in node.Children[i].All)
                {
                    eTag[i][e.TagName] = (eTag[i].ContainsKey(e.TagName)) ? eTag[i][e.TagName] + 1 : 1;
                }
            }

            Dictionary<int, double> informationQuantity = new Dictionary<int, double>();
            for (int i = 0; i < eTag.Count; i++)
            {
                informationQuantity[i] = InformationQuantity(0, i, eTag) + InformationQuantity(i + 1, eTag.Count - 1, eTag);
            }

            int separator = informationQuantity.OrderBy(n => n.Value).Select(n => n.Key).First();
            for (int i = 0; i < node.Children.Count; i++)
            {
                if (i <= separator)
                {
                    _PostNode.Add(node.Children[i]);
                }
                else
                {
                    _CommentNode.Add(node.Children[i]);
                }
            }
        }

        static double InformationQuantity(int nodeIndexFirst, int nodeIndexLast, Dictionary<int, Dictionary<string, int>> eTag)
        {
            Dictionary<string, int> dicTag = new Dictionary<string, int>();
            for (int i = nodeIndexFirst; i <= nodeIndexLast; i++)
            {
                foreach (string tag in eTag[i].Keys)
                {
                    dicTag[tag] = (dicTag.ContainsKey(tag)) ? dicTag[tag] + eTag[i][tag] : eTag[i][tag];
                }
            }

            // Eq.3, p.303
            double informationQuantity = 0;
            foreach (string tag in dicTag.Keys)
            {
                informationQuantity += -1 * dicTag[tag] * Math.Log((double)dicTag[tag] / dicTag.Values.Sum(), 2);
            }

            return informationQuantity;
        }
    }
}
