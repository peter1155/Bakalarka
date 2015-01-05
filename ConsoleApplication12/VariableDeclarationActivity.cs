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
    class VariableDeclarationActivity
    {
        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            if(navigator.Name == "unit")
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

        private String getType(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "decl_stmt") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");

            return findInSource("source_data1.xml", id, manager);
        }

        public void writeActionVariableDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            String type = getType(manager, navigator.Clone());

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "variable_declaration_deleted"),
                    new XElement("type", "variable"),
                    functionElement,
                    new XElement("variable",
                        new XElement("name",name),
                        new XElement("type",type)),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public XElement getSpecialType(XPathNavigator navigator, XmlNamespaceManager manager)
        {
            while(String.Compare(navigator.Name, "unit") != 0 &&  String.Compare(navigator.Name, "struct") !=0
                && String.Compare(navigator.Name, "union") !=0 )
            {
                navigator.MoveToParent();
            }
            if (navigator.Name == "unit")
                return new XElement("anonym_type","unknown");
            String name = navigator.Name;
            XElement specialType = new XElement("anonym_type",name);
            return specialType;
        }

        public void writeActionAnonymVariableDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            // Nazov typu
            XElement type = getSpecialType(navigator.Clone(), manager);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "variable_declaration_deleted"),
                    new XElement("type", "variable"),
                    functionElement,
                    new XElement("variable",
                        new XElement("name", name),
                        new XElement("type", type)),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        private String findConstantValue(String fileName, String name,XmlNamespaceManager manager)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();
            XPathNodeIterator nodes = navigator.Select("//cpp:define[not(cpp:macro/base:parameter_list) and cpp:macro/base:name='" + name + "']/cpp:value", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;
            else
                return nodes.Current.Value;
        }

        public void writeConstantDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            // Nazov typu
            String value = findConstantValue("source_data1.xml", name, manager);
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "variable_declaration_deleted"),
                    new XElement("type", "constant"),
                    new XElement("constant",
                        new XElement("name", name),
                        new XElement("value", value)),
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

            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt[@id='" + id + "']/base:decl[1]/base:type", manager);
            nodes.MoveNext();
            return nodes.Current.Value;
        }

        public void findVariableRemoved(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt[@diff:status]"
                + "/base:decl[@diff:status='removed' and not(ancestor::base:typedef) and not(ancestor::base:union) and not(ancestor::base:struct)]/base:name", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionVariableDeleted(manager, nodesNavigator);
            }

            nodes = navigator.Select("//base:struct[@diff:status='removed']/base:decl[@diff:status='removed']/base:name[@diff:status='removed']"
                + "| //base:union[@diff:status='removed']/base:decl[@diff:status='removed']/base:name[@diff:status='removed']", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionAnonymVariableDeleted(manager, nodesNavigator);
            }

            nodes = navigator.Select("//cpp:macro[@diff:status='removed' and not(base:parameter_list)]/base:name", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeConstantDeleted(manager, nodesNavigator);
            }
        }
    }
}
