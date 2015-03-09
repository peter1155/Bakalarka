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
        // Hlada nazov funkcie v ktorej je to vnorene a vytvara prislusny function element
        private XElement GetFunctionNameElement(XPathNavigator navigator)
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

        // Hlada poziciu na ktorej sa nachadza element s nazvom elementName
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

        // Vracia typ vymazanej premennej ako string 
        private String GetType(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, "decl_stmt") != 0)
            {
                navigator.MoveToParent();
            }

            String id = navigator.GetAttribute("id", "");

            return FindInSource("source_data1.xml", id, manager);
        }

        // Zistuje ci sa v skutocnosti nejedna o zmenu typu nie o vymazanie premennej
        private bool FindTypeChange(String varName, XElement element, XPathNavigator navigator, XmlNamespaceManager manager)
        {
            XPathNodeIterator nodes;
            // Ziska nazov funkcie v druhej verzii
            if (!element.ToString().Contains("global_variable"))
            {
                XNode functionNameAfter = element.LastNode;
                String temp = functionNameAfter.ToString();
                temp = temp.Replace("<after>", "");
                temp = temp.Replace("</after>", "");
                nodes = navigator.Select("//base:function[base:name='" + temp + "']//base:decl_stmt/base:decl[base:name='" + varName + " and not(@diff:status)']", manager);
            }
            else
            {
                nodes = navigator.Select("//base:decl_stmt/base:decl[base:name='" + varName + "' and not(ancestor::base:function) and not(@diff:status)]", manager);
            }
            
            if (nodes.Count > 0)
                return true;
            else
                return false;
        }

        // Zapisuje identifikovane zmazanie premennej do vystupneho xml suboru
        public void WriteActionVariableDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = FindPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            String type = GetType(manager, navigator.Clone());

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = GetFunctionNameElement(navigator.Clone());

            // V pripade ze sa jedna iba o zmenu typu neidentifikuj vymazanie premennej
            if (FindTypeChange(name, functionElement, navigator.Clone(), manager))
                return;

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

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

        // Vytvara element specialneho typu pre anonymnu strukturu alebo anonymny union
        public XElement GetSpecialType(XPathNavigator navigator, XmlNamespaceManager manager)
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

        // Zapisuje akciu zmazanie premennej anonymneho typu do xml suboru
        public void WriteActionAnonymVariableDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = FindPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            // Nazov typu
            XElement type = GetSpecialType(navigator.Clone(), manager);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = GetFunctionNameElement(navigator.Clone());

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

        // Vrati hodnotu konstanty s nazvom name ktora sa nachadza v subore fileName ako string  
        private String FindConstantValue(String fileName, String name,XmlNamespaceManager manager)
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

        // Zapise akciu vymazanie konstanty do vystupneho xml suboru
        public void WriteConstantDeleted(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu if
            List<String> list = FindPosition("name", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);
            // Nazov premennej
            String name = navigator.Value;
            // Nazov typu
            String value = FindConstantValue("source_data1.xml", name, manager);
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

        // Vracia typ vymazanej premennej na zaklade idecka a nazvu suboru fileName
        private String FindInSource(String fileName, String id, XmlNamespaceManager manager)
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

        // Identifikuje akciu vymazanie premennej, konstanty a zaznamena ju do vystupneho xml suboru
        public void FindVariableRemoved(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Identifikacia vymazania premennej 
            XPathNodeIterator nodes = navigator.Select("//base:function[not(@diff:status='removed')]//base:decl_stmt[@diff:status]"
                + "/base:decl[@diff:status='removed' and not(ancestor::base:typedef) and not(ancestor::base:union) and not(ancestor::base:struct)]/base:name[@diff:status='removed']" +
                " | //base:decl_stmt[@diff:status]"
                + "/base:decl[@diff:status='removed' and not(ancestor::base:function) and not(ancestor::base:typedef) and not(ancestor::base:union) and not(ancestor::base:struct)]/base:name[@diff:status='removed']", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteActionVariableDeleted(manager, nodesNavigator);
            }

            // Identifikacia zmazania premennych anonymnych typov (anonymna struktura a anonymny union)
            nodes = navigator.Select("//base:struct[@diff:status='removed']/base:decl[@diff:status='removed']/base:name[@diff:status='removed']"
                + "| //base:union[@diff:status='removed']/base:decl[@diff:status='removed']/base:name[@diff:status='removed']", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteActionAnonymVariableDeleted(manager, nodesNavigator);
            }

            // Identifikacia vymazania zadefinovanej konstanty
            nodes = navigator.Select("//base:function[not(@diff:status='removed')]//cpp:macro[@diff:status='removed' and not(base:parameter_list)]/base:name[@diff:status='removed']" +
                " | //cpp:macro[@diff:status='removed' and not(base:parameter_list)]/base:name[@diff:status='removed']", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteConstantDeleted(manager, nodesNavigator);
            }
        }
    }
}
