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
    class SpecialCaseActivity
    {
        public void writeActionPrintout(XPathNavigator navigator)
        {
            String diffType = navigator.GetAttribute("status", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");
            String type = navigator.Name;       // Nazov elementu {literal/name}
            String change = navigator.Value;    // Zmena vo formate before~after
            char[] delimiters = { '~' };
            string[] beforeAfterValues = change.Split(delimiters); // Rozsekam zmenu na casti

            if (String.Compare(type, "lit:literal") == 0)
                type = "format string";
            else if (String.Compare(type, "op:operator") == 0)
                type = "operator";
            else if (String.Compare(type, "name") == 0)
                type = "variable";

            // Zistujem v ktorej funkcii je to vnorene
            XPathNavigator tempNavigator = navigator.Clone();
            while (tempNavigator != null && String.Compare(tempNavigator.Name, "function") != 0)
            {
                tempNavigator.MoveToParent();
            }

            XPathNodeIterator function_childeren =
                tempNavigator.SelectChildren(XPathNodeType.Element);

            while (function_childeren.MoveNext() && function_childeren.Current.Name != "name") ;
            XPathNavigator functionNameNav = function_childeren.Current;

            // Zistujem poziciu podmienky if
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "if") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            String line = tempNavigator2.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = tempNavigator2.GetAttribute("column", "http://www.sdml.info/srcML/position");

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            if (String.Compare(diffType, "modified") == 0)
            {
                XElement my_element = new XElement("acion",
                    new XElement("name", "Input change"),
                    new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
                xdoc.Root.Add(my_element);
            }
            else if (String.Compare(diffType, "added") == 0)
            {
                XElement my_element = new XElement("acion",
                    new XElement("name", "Input change"),
                    new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("parameter", change),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
                xdoc.Root.Add(my_element);
            }
            else if (String.Compare(diffType, "removed") == 0)
            {
                XElement my_element = new XElement("acion",
                    new XElement("name", "Input change"),
                    new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("parameter", change),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
                xdoc.Root.Add(my_element);
            }
            xdoc.Save("RecordedActions.xml");
        }

        public void findDifferenceInOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:if[@diff:status]/base:then/base:block[base:return[@diff:status]" +
                " or base:break[@diff:status] or base:continue[@diff:status]] | //base:if[@diff:status]/base:then[base:return[@diff:status] or base:continue[@diff:status] or " +
                " base:break[@diff:status]]", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;

                XPathNodeIterator nodesText =
                   nodesNavigator.SelectDescendants(XPathNodeType.Element, false);

                while (nodesText.MoveNext())
                {
                    Console.WriteLine("\n" + nodesText.Current.Value);
                    writeActionPrintout(nodesText.Current);
                }
            }
            System.Console.ReadLine();
        }
    }
}
