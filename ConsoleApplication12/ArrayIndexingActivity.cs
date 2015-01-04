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
    class ArrayIndexingActivity
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

        private List<String> getType(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "decl_stmt") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");
            List<String> types = new List<string>();
            String typeBefore = findTypeInSource("source_data1.xml", id, manager);
            String typeAfter = findTypeInSource("source_data2.xml", id, manager);
            types.Add(typeBefore);
            types.Add(typeAfter);
            return types;
        }

        private List<String> getName(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            String id = navigator.GetAttribute("id", "");
            List<String> names = new List<string>();
            String nameBefore = findNameInSource("source_data1.xml", id, manager);
            String nameAfter = findNameInSource("source_data2.xml", id, manager);
            names.Add(nameBefore);
            names.Add(nameAfter);
            return names;
        }

        private List<String> getNameExpresion(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            String id = navigator.GetAttribute("id", "");
            List<String> names = new List<string>();
            String nameBefore = findNameInSourceExpresion("source_data1.xml", id, manager);
            String nameAfter = findNameInSourceExpresion("source_data2.xml", id, manager);
            names.Add(nameBefore);
            names.Add(nameAfter);
            return names;
        }

        private List<String> getTypeExpression(XmlNamespaceManager manager, XPathNavigator navigator, XElement funcElement)
        {
            navigator.MoveToChild(XPathNodeType.Element);

            if (funcElement.Value == "")
            {
                String before = findTypeInSourceExpresion2("source_data1.xml", navigator.Value, manager);
                String after = findTypeInSourceExpresion2("source_data2.xml", navigator.Value, manager);
                List<String> list = new List<string>();
                list.Add(before);
                list.Add(after);
                return list;
            }
            else
            {
                var temp = funcElement.Descendants();
                String before = findTypeInSourceExpresion1("source_data1.xml", navigator.Value, manager,temp.ElementAt(0).Value);
                String after = findTypeInSourceExpresion1("source_data2.xml", navigator.Value, manager,temp.ElementAt(1).Value);
                if(before==null || after==null )
                {
                    before = findTypeInSourceExpresion2("source_data1.xml", navigator.Value, manager);
                    after = findTypeInSourceExpresion2("source_data2.xml", navigator.Value, manager);
                }
                List<String> list = new List<string>();
                list.Add(before);
                list.Add(after);
                return list;
            }
        }

        private String findTypeInSource(String fileName, String id, XmlNamespaceManager manager)
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

        private String findNameInSource(String fileName, String id, XmlNamespaceManager manager)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt/base:decl/base:name[@id='" + id + "']", manager);
            nodes.MoveNext();
            return nodes.Current.Value;
        }

        private String findNameInSourceExpresion(String fileName, String id, XmlNamespaceManager manager)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:expr/base:name[@id='" + id + "']", manager);
            nodes.MoveNext();
            return nodes.Current.Value;
        }

        // Lokalna premenna
        private String findTypeInSourceExpresion1(String fileName, String name, XmlNamespaceManager manager, String func)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:function[base:name='"+func+"']//base:decl_stmt/base:decl[base:name/base:name='" + name + "']/base:type", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;
            return nodes.Current.Value;
        }

        // Globalna premenna
        private String findTypeInSourceExpresion2(String fileName, String name, XmlNamespaceManager manager)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt/base:decl[base:name/base:name='" + name + "']/base:type", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;
            return nodes.Current.Value;
        }

        public void writeActionArrayDeclModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            List<String> names = getName(manager, navigator.Clone());
            List<String> types = getType(manager, navigator.Clone());

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("acion",
                    new XElement("name", "array_indexing"),
                    new XElement("type", "declaration"),
                    functionElement,
                    new XElement("variable",
                        new XElement("name",
                            new XElement("before", names.ElementAt(0)),
                            new XElement("after", names.ElementAt(1))),
                        new XElement("type",
                            new XElement("before", types.ElementAt(0)),
                            new XElement("after", types.ElementAt(1)))),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionArrayIndexModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            List<String> names = getNameExpresion(manager, navigator.Clone());
            

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());
            
            // Typ pola
            List<String> types = getTypeExpression(manager, navigator.Clone(),functionElement);
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("acion",
                    new XElement("name", "array_indexing"),
                    new XElement("type", "expresion"),
                    functionElement,
                    new XElement("variable",
                        new XElement("name",
                            new XElement("before", names.ElementAt(0)),
                            new XElement("after", names.ElementAt(1))),
                        new XElement("type",
                            new XElement("before", types.ElementAt(0)),
                            new XElement("after", types.ElementAt(1)))),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void findArrayIndexingModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:decl[@diff:status]/base:name[base:index and (base:index/@diff:status='modified'" 
                +" or base:index/@diff:status='below')]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionArrayDeclModification(manager, nodesNavigator);
            }

            nodes = navigator.Select("//base:expr[@diff:status]/base:name[base:index and (base:index/@diff:status='modified'"
                + " or base:index/@diff:status='below')]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionArrayIndexModification(manager, nodesNavigator);
            }
            
        }
    }
}
