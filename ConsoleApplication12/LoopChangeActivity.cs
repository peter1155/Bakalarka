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
    class LoopChangeActivity
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

        private List<String> getConditionBeforeAfter(XmlNamespaceManager manager, XPathNavigator navigator, String elementName)
        {
            while (navigator != null && String.Compare(navigator.Name, elementName) != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");
            String conditionBefore = findInSource("source_data1.xml", id, manager,elementName);
            String conditionAfter = findInSource("source_data2.xml", id, manager,elementName);
            // Mazem otvaraciu a zatvaraciu zatvorku
            conditionBefore = conditionBefore.Substring(1, conditionBefore.Length - 2);
            conditionAfter = conditionAfter.Substring(1, conditionAfter.Length - 2);

            List<String> list = new List<string>();
            list.Add(conditionBefore);
            list.Add(conditionAfter);

            return list;
        }

        private String getParent(XPathNavigator navigator)
        {
            navigator.MoveToParent();
            return navigator.Name;
        }

        private String findInSource(String fileName, String id, XmlNamespaceManager manager, String elementName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:" + elementName + "[@id='" + id + "']/base:condition", manager);
            nodes.MoveNext();
            return nodes.Current.Value;
        }

        public void writeActionLoopChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            String parent = getParent(navigator.Clone());
            // Zistujem poziciu if
            List<String> list = findPosition("condition", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem podmienku pred zmenou a po zmene 
            list = getConditionBeforeAfter(manager, navigator.Clone(),parent);
            String conditionBefore = list.ElementAt(0);
            String conditionAfter = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("acion",
                    new XElement("name", "loop_condition_change"),
                    new XElement("type", parent),
                    functionElement,
                    new XElement("condition",
                        new XElement("condition_before", conditionBefore),
                        new XElement("condition_after", conditionAfter)),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Specialne pre for cyklus lebo ma trocha ine elementy
        public void writeActionLoopChangeFor(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            //String parent = getParent(navigator.Clone());
            // Zistujem poziciu if
            List<String> list = findPosition("for", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem podmienku pred zmenou a po zmene 
            XElement control = getConditionBeforeAfterFor(manager, navigator.Clone());
            
            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("acion",
                    new XElement("name", "loop_condition_change"),
                    new XElement("type", "for"),
                    functionElement,
                    control,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        private XElement getConditionBeforeAfterFor(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "for") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");

            String conditionBefore = findInSourceFor("source_data1.xml", id, manager, "condition");
            String initBefore = findInSourceFor("source_data1.xml", id, manager, "init");
            String incrBefore = findInSourceFor("source_data1.xml", id, manager, "incr");

            String conditionAfter = findInSourceFor("source_data2.xml", id, manager, "condition");
            String initAfter = findInSourceFor("source_data2.xml", id, manager, "init");
            String incrAfter = findInSourceFor("source_data2.xml", id, manager, "incr");

            XElement control = new XElement("control",
                new XElement("init",
                    new XElement("before",initBefore),
                    new XElement("after",initAfter)),
                new XElement("condition",
                    new XElement("before",conditionBefore),
                    new XElement("after",conditionAfter)),
                new XElement("incr",
                    new XElement("before",incrBefore),
                    new XElement("after",incrAfter)));

            return control;
        }

        private String findInSourceFor(String fileName, String id, XmlNamespaceManager manager,String elementName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:for[@id='" + id + "']/base:"+elementName, manager);
            nodes.MoveNext();
            return nodes.Current.Value;
        }

        public void findLoopChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:while[@diff:status='below' or @diff:status='modified']/base:condition[@diff:status]"
                + " | //base:do[@diff:status='below' or @diff:status='modified']/base:condition[@diff:status]",manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionLoopChange(manager, nodesNavigator);
            }

            nodes = navigator.Select("//base:for[(@diff:status='below' or @diff:status='modified') and"
            + " (base:init/@diff:status or base:incr/@diff:status or base:condition/@diff:status)]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionLoopChangeFor(manager, nodesNavigator);
            }
        }
    }
}
