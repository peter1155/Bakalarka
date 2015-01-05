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
    class ConditionChangeActivity
    {
        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (navigator != null && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
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

        public List<String> findPosition(String elementName, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, elementName) != 0)
            {
                navigator.MoveToParent();
            }

            String line = navigator.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = navigator.GetAttribute("column", "http://www.sdml.info/srcML/position");
            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            return list;
        }

        private List<String> getConditionBeforeAfter(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "if") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");
            String conditionBefore = findInSource("source_data1.xml", id, manager);
            String conditionAfter = findInSource("source_data2.xml", id, manager);
            // Mazem otvaraciu a zatvaraciu zatvorku
            conditionBefore = conditionBefore.Substring(1, conditionBefore.Length - 2);
            conditionAfter = conditionAfter.Substring(1, conditionAfter.Length - 2);
            
            List<String> list = new List<string>();
            list.Add(conditionBefore);
            list.Add(conditionAfter);
            
            return list;
        }

        public void writeActionConditionChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition("if", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            
            // Zistujem podmienku pred zmenou a po zmene 
            list = getConditionBeforeAfter(manager, navigator.Clone());
            String conditionBefore = list.ElementAt(0);
            String conditionAfter = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "condition_change"),
                    new XElement("type", "condition_modified"),
                    functionElement,
                    new XElement("condition",
                        new XElement("condition_before",conditionBefore),
                        new XElement("condition_after",conditionAfter)),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionConditionChangeIfElse(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem na ktorej vetve nastala zmena
            String branch = navigator.Name;
            
            // Zistujem poziciu if
            List<String> list = findPosition("if", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "condition_change"),
                    new XElement("type", "body_modified"),
                    new XElement("branch", branch),
                    functionElement,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionConditionChangeElse(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            String type = navigator.GetAttribute("status","http://www.via.ecp.fr/~remi/soft/xml/xmldiff");
            // Zistujem poziciu if
            List<String> list = findPosition("if", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "condition_change"),
                    new XElement("type", "else_added/removed"),
                    new XElement("diff_type",type),
                    functionElement,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        private String findInSource(String fileName, String id, XmlNamespaceManager manager)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:if[@id='"+id+"']/base:condition", manager);
            nodes.MoveNext();
            /*if (nodes.Count == 0)
                return null;
            else
                return nodes.Current.Value;*/
            return nodes.Current.Value;
        }

        public void findConditionChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:if[@diff:status='below']/base:condition[@diff:status]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionConditionChange(manager, nodesNavigator);
            }

            nodes = navigator.Select("//base:if[@diff:status='below']/base:then[@diff:status='below'] | "
                +"//base:if[@diff:status='below']/base:else[@diff:status='below'] | "
                +"//base:if[@diff:status='below']/base:elseif[@diff:status='below']", manager);
                
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionConditionChangeIfElse(manager, nodesNavigator);
            }

            nodes = navigator.Select("//base:if[@diff:status='below']/base:else[@diff:status='added' or @diff:status='removed'] | "
                + "//base:if[@diff:status='below']/base:elseif[@diff:status='added' or @diff:status='removed']", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionConditionChangeElse(manager, nodesNavigator);
            }
        }
    }
}
