using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ContentExtraction
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //string[] experimentFiles = Directory.GetFiles("C:/Users/ceekz/Desktop/file", "*.html_marked.html");
            string[] experimentFiles = { "http://googlewebmastercentral.blogspot.jp/2012/12/webmaster-tools-verification-strategies.html" };
            string resultDirectory = "C:/Users/ceekz/Desktop";

            foreach (string file in experimentFiles)
            {
                //if (File.Exists(resultDirectory + Path.GetFileName(file)))
                //{
                //    continue;
                //}

                Console.WriteLine(DateTime.Now + " " + file);

                // Content Extraction
                ContentExtractionUsingLossRatio ce = new ContentExtractionUsingLossRatio(file);

                // Save Content, Post and Cmmment HTML
                //using (StreamWriter sw = new StreamWriter(resultDirectory + "/content.html"))
                //{
                //    foreach (HtmlElement e in ce.Content)
                //    {
                //        sw.WriteLine(e.OuterHtml);
                //    }
                //}
                //using (StreamWriter sw = new StreamWriter(resultDirectory + "/post.html"))
                //{
                //    foreach (HtmlElement e in ce.Post)
                //    {
                //        sw.WriteLine(e.OuterHtml);
                //    }
                //}
                //using (StreamWriter sw = new StreamWriter(resultDirectory + "/comment.html"))
                //{
                //    foreach (HtmlElement e in ce.Comment)
                //    {
                //        sw.WriteLine(e.OuterHtml);
                //    }
                //}

                // Save log and annotated html for Experiment
                using (StreamWriter sw = new StreamWriter(resultDirectory + "/result.txt", true))
                {
                    sw.WriteLine(file);
                    sw.WriteLine(OutputLine("post", ce.Post));
                    sw.WriteLine(OutputLine("comment", ce.Comment));
                }
                using (StreamWriter sw = new StreamWriter(resultDirectory + "/" + Path.GetFileName(file)))
                {
                    AnnotateNode(ce.Node, ce.LossRatio, ce.Content, ce.Post, ce.Comment);

                    sw.WriteLine(ce.Node.OuterHtml);
                }

                Console.WriteLine(string.Format("  Post:{0} Comment:{1}", ce.Post.Count, ce.Comment.Count));
            }
        }

        static string OutputLine(string name, List<HtmlElement> listNode)
        {
            string line = "";

            foreach (HtmlElement node in listNode)
            {
                if (!node.GetAttribute("block").Equals(""))
                {
                    line += string.Format("{0}:{1} ", name, node.GetAttribute("block"));
                }
                foreach (HtmlElement e in node.All)
                {
                    if (!e.GetAttribute("block").Equals(""))
                    {
                        line += string.Format("{0}:{1} ", name, e.GetAttribute("block"));
                    }
                }
            }

            return line;
        }

        static void AnnotateNode(HtmlElement Node, List<Tuple<HtmlElement, double>> LossRatio, List<HtmlElement> Content, List<HtmlElement> Post, List<HtmlElement> Comment)
        {
            Dictionary<HtmlElement, Tuple<int, double>> dicLossRatio = new Dictionary<HtmlElement, Tuple<int, double>>();
            foreach (Tuple<HtmlElement, double> lossRatio in LossRatio)
            {
                dicLossRatio[lossRatio.Item1] = new Tuple<int, double>(dicLossRatio.Count, lossRatio.Item2);
            }

            foreach (HtmlElement e in Node.All)
            {
                if (dicLossRatio.ContainsKey(e))
                {
                    e.Style = "border: 1px solid red;";
                    e.SetAttribute("title", string.Format("{0}: {1} (avg: {2})", dicLossRatio[e].Item1, dicLossRatio[e].Item2, LossRatio.Select(n => n.Item2).Average()));
                }
                if (Content.Contains(e))
                {
                    e.Style = "background-color:#faa;";
                }
                if (Post.Contains(e))
                {
                    e.Style = "background-color:#afa;";
                }
                if (Comment.Contains(e))
                {
                    e.Style = "background-color:#aaf;";
                }
            }
        }
    }
}
