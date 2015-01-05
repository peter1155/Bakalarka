using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
namespace ConsoleApplication12
{
    class OutputCanceledActivity
    {
        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            if (navigator.Name == "unit")
            {
                return new XElement("global_variable");
            }

            // Ziska deti elementu function
            XPathNodeIterator function_childeren = navigator.SelectChildren(XPathNodeType.Element);

            // Najde elemnt name elementu function 
            while (function_childeren.MoveNext() && function_childeren.Current.Name != "name") ;

            XPathNavigator functionNameNavigator = function_childeren.Current;
            XElement functionElement = null;
            String funcName = functionNameNavigator.Value;

            // Treba osetrit pripad ze nazov funkcie sa rapidne zmenil a je povazovany za zmazany a pridany
            List<String> funcNames = new List<string>();
            funcNames.Add(funcName);
            while (function_childeren.MoveNext() && function_childeren.Current.Name == "name")
            {
                funcNames.Add(function_childeren.Current.Value);
            }

            // Ak doslo k modifikacii nazvu funkcie treba to osetrit
            if (funcName.Contains("~"))
            {
                char[] del = { '~' };
                string[] beforeAfterValues = funcName.Split(del);
                functionElement = new XElement("function_name",
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]));
            }
            else if (funcNames.Count > 1)
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcNames.ElementAt(1)),
                    new XElement("after", funcNames.ElementAt(0)));
            }
            else
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcName),
                    new XElement("after", funcName));
            }
            return functionElement;
        }

        public List<String> findPosition(XPathNavigator navigator)
        {

            // Pohne sa smerom k vnorenemu name, ktore ma dane atributy
            navigator.MoveToChild(XPathNodeType.Element);

            String line = navigator.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = navigator.GetAttribute("column", "http://www.sdml.info/srcML/position");
            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            return list;
        }

        public void writeActionCanceledOutputPrintf(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());           

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "temp_output_canceled"),
                    new XElement("type", "printf"),
                    functionElement,
                    new XElement("canceled_function",navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionCanceledOutputPutchar(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "temp_output_canceled"),
                    new XElement("type", "putchar"),
                    functionElement,
                    new XElement("canceled_function", navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        private String processCommentNodePrintf(XPathNavigator navigator)
        {
            String comment = navigator.Value;
            
            if (!comment.Contains("printf("))
                return null;
            int index = comment.IndexOf("printf(");
            comment = comment.Substring(index);
            index = comment.IndexOf(");");
            comment = comment.Substring(0, index+1);
            return comment;
        }

        private String processCommentNodePutchar(XPathNavigator navigator)
        {
            String comment = navigator.Value;

            if (!comment.Contains("putchar("))
                return null;
            int index = comment.IndexOf("putchar(");
            comment = comment.Substring(index);
            index = comment.IndexOf(");");
            comment = comment.Substring(0, index+1);
            return comment;
        }

        // Vracia maximum dvoch cisel
        private int max(int a, int b)
        {
            return (a > b) ? a : b;
        }

        // Longest common subsequence
        private int LCS(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return 0;
            }

            int[,] table = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                table[i, 0] = 0;
            for (int i = 0; i <= s2.Length; i++)
                table[0, i] = 0;

            for (int i = 1; i <= s1.Length; i++)
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                        table[i, j] = table[i - 1, j - 1] + 1;
                    else
                    {
                        table[i, j] = max(table[i - 1, j], table[i, j - 1]);
                    }

                }
            return table[s1.Length, s2.Length];
        }

        public void findCanaceledOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodesComment = navigator.Select("//base:comment[@diff:status='added']", manager);
            XPathNodeIterator nodesPrintf = navigator.Select("//base:call[base:name='printf' and  @diff:status='removed']", manager);
            XPathNodeIterator nodesPutchar = navigator.Select("//base:call[base:name='putchar' and  @diff:status='removed']", manager);

            List<String> commentedPrintf = new List<string>();
            List<String> commentedPutchar = new List<string>();

            while (nodesComment.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesComment.Current;

                String temp = processCommentNodePrintf(nodesNavigator);
                if (temp != null)
                    commentedPrintf.Add(temp);
                temp = processCommentNodePutchar(nodesNavigator);
                if (temp != null)
                    commentedPutchar.Add(temp);
            }

            // Hladam zhody medzi printf v komentaroch s printf, ktore su zaznamenane ako zmazane z prvej verzie
            while (nodesPrintf.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesPrintf.Current;
                for(int i =0;i< commentedPrintf.Count;i++)
                {
                    int subsequence = LCS(commentedPrintf.ElementAt(i), nodesNavigator.Value);
                    float similarity = subsequence / (float)((commentedPrintf.ElementAt(i).Length + nodesNavigator.Value.Length) / 2f);
                    
                    if(similarity > 0.85)
                    {
                        writeActionCanceledOutputPrintf(manager, nodesNavigator);
                    }
                } 
            }

            while (nodesPutchar.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesPutchar.Current;
                for (int i = 0; i < commentedPutchar.Count; i++)
                {
                    int subsequence = LCS(commentedPutchar.ElementAt(i), nodesNavigator.Value);
                    float similarity = subsequence / (float)((commentedPutchar.ElementAt(i).Length + nodesNavigator.Value.Length) / 2f);

                    if (similarity > 0.85)
                    {
                        writeActionCanceledOutputPutchar(manager, nodesNavigator);
                    }
                }
            }
        }
    }
}
