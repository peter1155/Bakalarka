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
using System.Text.RegularExpressions;

namespace ConsoleApplication12
{
    class CommentActivity
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
                return new XElement("global_comment");
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
            String line = navigator.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = navigator.GetAttribute("column", "http://www.sdml.info/srcML/position");
            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            return list;
        }

        public void writeActionCommentAdded(XmlNamespaceManager manager, XPathNavigator navigator)
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
            XElement my_element = new XElement("acion",
                    new XElement("name", "comment"),
                    new XElement("type", "added"),
                    functionElement,
                    new XElement("commented_source", navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionCommentRemoved(XmlNamespaceManager manager, XPathNavigator navigator)
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
            XElement my_element = new XElement("acion",
                    new XElement("name", "comment"),
                    new XElement("type", "deleted"),
                    functionElement,
                    new XElement("commented_source", navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
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

        private String prcessXmlFile(String fileName)
        {
            XmlDocument doc1 = new XmlDocument();
            doc1.Load(fileName);
            attributeDelete(doc1.ChildNodes);
            return doc1.InnerXml;
        }

        private void attributeDelete(XmlNodeList list1)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement node = (XmlElement)listNode;
                    
                    node.RemoveAllAttributes();
                    
                    if (listNode.HasChildNodes)
                        attributeDelete(listNode.ChildNodes);
                }
        }

        private String removeNamespaces(String temp)
        {
            temp = temp.Replace("xmlns=\"http://www.sdml.info/srcML/src\"", "");
            temp = temp.Replace("xmlns:cpp=\"http://www.sdml.info/srcML/cpp\"", "");
            temp = temp.Replace("xmlns:lit=\"http://www.sdml.info/srcML/literal\"", "");
            temp = temp.Replace("xmlns:op=\"http://www.sdml.info/srcML/operator\"", "");
            temp = temp.Replace("xmlns:type=\"http://www.sdml.info/srcML/modifier\"", "");
            temp = temp.Replace("xmlns:pos=\"http://www.sdml.info/srcML/position\"", "");
            temp = temp.Replace("xmlns:diff=\"http://www.via.ecp.fr/~remi/soft/xml/xmldiff\"", "");
            temp = Regex.Replace(temp, @"\s+", "");
            return temp;
        }

        public void findCanaceledOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodesCommentAdded = navigator.Select("//base:comment[@diff:status='added']", manager);
            XPathNodeIterator nodesCommentRemoved = navigator.Select("//base:comment[@diff:status='removed']", manager);
            
            // Spracujem xml prvej a druhej verzie 
            String firstSource = prcessXmlFile("source_data1.xml");
            String secondSource = prcessXmlFile("source_data2.xml");

            firstSource = removeNamespaces(firstSource);
            secondSource = removeNamespaces(secondSource);
            
            while (nodesCommentAdded.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesCommentAdded.Current;

                // Vygenerujem xml z obsahu komentara
                String temp = nodesNavigator.Value;
                temp = temp.Replace("//","");
                temp = temp.Replace("/*", "");
                temp = temp.Replace("*/", "");
                ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();
                String xmlString = my_runner.GenerateSrcMLFromString(temp);

                // Este treba zmazat atributy

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);
                attributeDelete(doc.ChildNodes);
                temp = "";
            
                var children = doc.ChildNodes;
                foreach(XmlNode child in children)
                {
                    temp += child.InnerXml;
                }

                temp = removeNamespaces(temp);
                
                if(firstSource.Contains(temp))
                {
                    writeActionCommentAdded(manager, nodesNavigator);
                }
            }

            while (nodesCommentRemoved.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesCommentRemoved.Current;

                // Vygenerujem xml z obsahu komentara
                String temp = nodesNavigator.Value;
                temp = temp.Replace("//", "");
                temp = temp.Replace("/*", "");
                temp = temp.Replace("*/", "");
                ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();
                String xmlString = my_runner.GenerateSrcMLFromString(temp);

                // Este treba zmazat atributy

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);
                attributeDelete(doc.ChildNodes);
                temp = "";

                var children = doc.ChildNodes;
                foreach (XmlNode child in children)
                {
                    temp += child.InnerXml;
                }

                temp = removeNamespaces(temp);

                if (secondSource.Contains(temp))
                {
                    writeActionCommentRemoved(manager, nodesNavigator);
                }
            }

        }
    }
}
