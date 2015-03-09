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
        // Ziska nazov funkcie v ktorej to je vnorene
        private XElement GetFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            // V pripade ze sa nenajde element function jena sa o chybu v kode
            if (navigator.Name == "unit")
                return new XElement("errorInSource");

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

        // Najde poziciu elementu elementName 
        public List<String> FindPosition(String elementName, XPathNavigator navigator)
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

        // Najde podmienku pred zmenou a po zmene
        private List<String> GetConditionBeforeAfter(XmlNamespaceManager manager, XPathNavigator navigator, String elementName)
        {
            while (navigator != null && String.Compare(navigator.Name, elementName) != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");

            // Ziska podmienku pred zmenou
            String conditionBefore = FindInSource("source_data1.xml", id, manager,elementName);
             // Ziska podmienku po zmene 
            String conditionAfter = FindInSource("source_data2.xml", id, manager,elementName);
            
            // Treba osetrit pripad ked sa navzajom namapuju odlisne typy cyklov for-while, for-do,do-while,while-do
            if (conditionBefore == null)
            {
                conditionBefore = FindInSourceFor("source_data1.xml", id, manager, "condition");
                if (conditionBefore == null)
                {
                    if (elementName == "while")
                        conditionBefore = FindInSource("source_data1.xml", id, manager, "do");
                    else
                        conditionBefore = FindInSource("source_data1.xml", id, manager, "while");
                }
                else
                    conditionBefore = " conversion for -> " + elementName + "(" + conditionBefore + ") "; // medzeri zamerne ...
                if (conditionBefore == null)
                    return null;
            }
            
            
            // Mazem otvaraciu a zatvaraciu zatvorku
            conditionBefore = conditionBefore.Substring(1, conditionBefore.Length - 2);
            conditionAfter = conditionAfter.Substring(1, conditionAfter.Length - 2);

            // Vracia podmienky ako string list
            List<String> list = new List<string>();
            list.Add(conditionBefore);
            list.Add(conditionAfter);

            return list;
        }

        // Vracia podmienku v cykle ktorej rodicovsky element ma nazov elementName a id = id
        // zo suboru s nazvom fileName ako string
        private String FindInSource(String fileName, String id, XmlNamespaceManager manager, String elementName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:" + elementName + "[@id='" + id + "']/base:condition", manager);
            nodes.MoveNext();
            if (nodes.Count > 0)
                return nodes.Current.Value;
            else return null;
        }
        
        // Dostane string a vymaze z neho whitespaces
        private String RemoveWhiteSpaces(String str)
        {
            str = str.Replace(" ", "");
            str = str.Replace("\t", "");
            str = str.Replace("\n", "");
            return str;
        }

        // Zapis zmenu v podmienke cyklu do vystupneho XML
        public void WriteActionLoopChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            String parent = navigator.Name;//= getParent(navigator.Clone());
            // Zistuje poziciu
            List<String> list = FindPosition(parent, navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistuje podmienku pred zmenou a po zmene 
            list = GetConditionBeforeAfter(manager, navigator.Clone(),parent);
            // Osetrenie pre pripad ze sa navzajom namapuju nespravne elmenty
            if (list == null)
                return;
            String conditionBefore = list.ElementAt(0);
            String conditionAfter = list.ElementAt(1);

            String tempCondBefore = conditionBefore;
            tempCondBefore = RemoveWhiteSpaces(tempCondBefore);
            String tempCondAfter = conditionAfter;
            tempCondAfter = RemoveWhiteSpaces(tempCondAfter);
            if (tempCondAfter == tempCondBefore)
                return;

            // Zistuje v ktorej funkcii je to vnorene
            XElement functionElement = GetFunctionNameElement(navigator.Clone());

            // Zapise akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
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

        // Zapis zmeny pre for cyklus, ktory ma ine elementy
        public void WriteActionLoopChangeFor(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistuje poziciu 
            List<String> list = FindPosition("for", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistuje podmienku pred zmenou a po zmene 
            XElement control = GetConditionBeforeAfterFor(manager, navigator.Clone());

            // Osetrenie pre pripad ze sa navzajom namapuju nespravne elementy napr. podmienka a cyklus
            if (control == null)
                return;

            // Zistuje v ktorej funkcii je to vnorene
            XElement functionElement = GetFunctionNameElement(navigator.Clone());

            // Zapise akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
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

        // Vracia element s podmienkou pred zmenou a po zmene specialne pre for cyklus ktoreho
        // riadiaca cast pozostava z troch elementov: init, incr, condition
        private XElement GetConditionBeforeAfterFor(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "for") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");

            String conditionBefore = FindInSourceFor("source_data1.xml", id, manager, "condition");
            String initBefore = FindInSourceFor("source_data1.xml", id, manager, "init");
            String incrBefore = FindInSourceFor("source_data1.xml", id, manager, "incr");

            String conditionAfter = FindInSourceFor("source_data2.xml", id, manager, "condition");
            String initAfter = FindInSourceFor("source_data2.xml", id, manager, "init");
            String incrAfter = FindInSourceFor("source_data2.xml", id, manager, "incr");

            // Treba osetrit ze sa nenamapovali na seba rovnake typy cyklov
            if(conditionBefore == null)
            {
                conditionBefore = FindInSource("source_data1.xml", id, manager, "while");
                if (conditionBefore == null)
                    conditionBefore = FindInSource("source_data1.xml", id, manager, "do");
                else
                    conditionBefore = "conversion while -> for" + conditionBefore;
                if (conditionBefore == null)
                    return null;
                else
                    conditionBefore = "conversion do_while -> for" + conditionBefore;
            }

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

        // Vracia obsah jednotlivych elementov kontrolnej casti for cyklu ako string 
        // podla nazvu elementName a idecka for cyklu
        private String FindInSourceFor(String fileName, String id, XmlNamespaceManager manager,String elementName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:for[@id='" + id + "']/base:"+elementName, manager);
            nodes.MoveNext();
            if (nodes.Count > 0)
                return nodes.Current.Value;
            else
                return null;
        }

        // Hlada a zapisuje zmeny v riadiacej casti cyklov for, do , while
        public void FindLoopChange(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Identifikacia zmien v riadiacej casti while a do while cyklov
            XPathNodeIterator nodes = navigator.Select("//base:while[(@diff:status='below' or @diff:status='modified') and base:condition[@diff:status]]"
                + " | //base:do[(@diff:status='below' or @diff:status='modified') and base:condition[@diff:status]]"
                + " | //base:do[(@diff:status='below' or @diff:status='modified') and base:condition[@similarity!='1']]"
                + " | //base:while[(@diff:status='below' or @diff:status='modified') and base:condition[@similarity!='1']]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteActionLoopChange(manager, nodesNavigator);
            }

            // Identifikacia zmien v riadiacej casti for cyklu
            nodes = navigator.Select("//base:for[(@diff:status='below' or @diff:status='modified') and"
            + " (base:init/@diff:status or base:incr/@diff:status or base:condition/@diff:status)]"
            + " | //base:for[(@diff:status='below' or @diff:status='modified') and base:init[@similarity!='1']]"  
            + " | //base:for[(@diff:status='below' or @diff:status='modified') and base:incr[@similarity!='1']]"
            + " | //base:for[(@diff:status='below' or @diff:status='modified') and base:condition[@similarity!='1']]", manager); 

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteActionLoopChangeFor(manager, nodesNavigator);
            }
        }
    }
}
