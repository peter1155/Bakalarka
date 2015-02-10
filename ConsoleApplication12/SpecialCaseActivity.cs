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
        // Vracia typ specialnej aktivity v zavislosti od toho ktory z prikazov
        // break, continue, return bol pridany do kodu
        public String specialCaseType(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = null; 
            nodes = navigator.Select("base:break",manager);
            if (nodes.Count > 0)
                return "break";
            nodes = navigator.Select("base:continue", manager);
            if (nodes.Count > 0)
                return "continue";
            nodes = navigator.Select("base:return", manager);
            if (nodes.Count > 0)
                return "return";
            return null;
        }

        // Ziskava nazov funkcie v ktorej je vnorena dana aktivita
        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

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

        // Vracia element mena konstanty voci ktorej prebehlo porovnanie
        private XElement getConstantNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (navigator != null && String.Compare(navigator.Name, "expr") != 0)
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
                functionElement = new XElement("name",
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]));
            }
            else if (funcNames.Count > 1)
            {
                functionElement = new XElement("name",
                    new XElement("before", funcNames.ElementAt(1)),
                    new XElement("after", funcNames.ElementAt(0)));
            }
            else
            {
                functionElement = new XElement("name",
                    new XElement("before", funcName),
                    new XElement("after", funcName));
            }
            return functionElement;
        }
        
        // Hlada poziciu elementu s nazvom elementName
        public List<String> findPosition(String elementName,XPathNavigator navigator)
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

        // Zapis identifikovanej aktivity pridanie jedneho z prikazov break, return, continue
        // do tela then vetvy
        public void writeActionSpecialCaseThen(XmlNamespaceManager manager,XPathNavigator navigator)
        {
            // Zistujem o ktory specialny pripad sa jedna
            String type = specialCaseType(manager, navigator.Clone());
           
            // Zistujem poziciu if
            List<String> list = findPosition("if", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "special_case"),
                    new XElement("branch", "then"),
                    new XElement("type", type),
                    functionElement,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Zapis identifikovanej aktivity pridanie jedneho z prikazov break, return, continue
        // do tela else vetvy
        public void writeActionSpecialCaseElse(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem o ktory specialny pripad sa jedna
            String type = specialCaseType(manager, navigator.Clone());

            // Zistujem poziciu if
            List<String> list = findPosition("else", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "special_case"),
                    new XElement("branch", "else"),
                    new XElement("type", type),
                    functionElement,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Zapis identifikovanej aktivity porovnanie voci konstante
        public void writeActionSpecialCaseConst(XmlNamespaceManager manager, XPathNavigator navigator, List<String> listConstant)
        {
            // Zistujem o ktory specialny pripad sa jedna
            XElement Xname = getConstantNameElement(navigator.Clone());
            String name = navigator.Value;
            int index = listConstant.IndexOf(name);
            String value = listConstant.ElementAt(index + 1);
            // Zistujem poziciu if
            List<String> list = findPosition("if", navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "special_case"),
                    new XElement("type", "constant_comparison"),
                    new XElement("constant",
                        //new XElement("name",name),
                        Xname,
                        new XElement("value",value)),
                    functionElement,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Najdenie hodnoty zadefinovanej konstanty
        private String findInSource(String fileName, String name)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();

            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("base", "http://www.sdml.info/srcML/src");
            manager.AddNamespace("cpp", "http://www.sdml.info/srcML/cpp");
            manager.AddNamespace("lit", "http://www.sdml.info/srcML/literal");
            manager.AddNamespace("op", "http://www.sdml.info/srcML/operator");
            manager.AddNamespace("type", "http://www.sdml.info/srcML/modifier");
            manager.AddNamespace("pos", "http://www.sdml.info/srcML/position");

            XPathNodeIterator nodes = navigator.Select("//cpp:define[not(cpp:macro/base:parameter_list) and cpp:macro/base:name='" + name + "']/cpp:value", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;
            else 
                return nodes.Current.Value;
        }
       
        // Vracia list vsetkych zadefinovanych konstant v programe
        private List<String> findDefinedConstants(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//cpp:macro[not(base:parameter_list)]/base:name", manager);
            List<String> list = new List<string>();
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                String constantName = nodesNavigator.Value;
                String constantValue = null;
                if (constantName.Contains("~"))
                {
                    char[] delimiters = {'~'};
                    string[] beforeAfterValues = constantName.Split(delimiters);
                    constantValue = findInSource("source_data1.xml", beforeAfterValues[0]);
                    if (constantValue == null)
                        constantValue = findInSource("source_data2.xml", beforeAfterValues[0]);
                    if (constantValue == null)
                        constantValue = findInSource("source_data1.xml", beforeAfterValues[1]);
                    if (constantValue == null)
                        constantValue = findInSource("source_data2.xml", beforeAfterValues[1]);
                    list.Add(constantName);
                    list.Add(constantValue);
                    list.Add(beforeAfterValues[0]);
                    list.Add(constantValue);
                    list.Add(beforeAfterValues[1]);
                    list.Add(constantValue);
                }
                else
                {
                    constantValue = findInSource("source_data1.xml", nodesNavigator.Value);
                    if (constantValue == null)
                        constantValue = findInSource("source_data2.xml", nodesNavigator.Value);
                    list.Add(constantName);
                    list.Add(constantValue);
                }
                
 
            }
            return list;
        }

        // Identifikuje aktivity pridanie specialneho pripadu (pridanie continue,break,return do tela if/else
        // alebo pridanie porovnania voci zadefinovanej konstante v podmienke if/else) a zapise do vystupneho xml
        public void findSpecialCaseAdd(XmlNamespaceManager manager, XPathNavigator navigator)
        { 
            // Najdenie pridania jedneho zo specialnych prikazov do then vetvy
            XPathNodeIterator nodes = navigator.Select("//base:if[@diff:status]/base:then/base:block[base:return[@diff:status='added']" +
                " or base:break[@diff:status='added'] or base:continue[@diff:status='added']] | //base:function[not(@diff:status='added')]//base:if[@diff:status]/base:then[base:return[@diff:status='added'] or base:continue[@diff:status='added'] or " +
                " base:break[@diff:status='added']]", manager);
            
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionSpecialCaseThen(manager,nodesNavigator);
            }

            // Najdenie pridania jedneho zo specialnych prikazov do else vetvy
            nodes = navigator.Select("//base:else[@diff:status]/base:block[base:return[@diff:status='added']" +
               " or base:break[@diff:status='added'] or base:continue[@diff:status='added']] | //base:function[not(@diff:status='added')]//base:else[@diff:status and ( base:return[@diff:status='added'] or base:continue[@diff:status='added'] or " +
               " base:break[@diff:status='added'])]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionSpecialCaseElse(manager, nodesNavigator);
            }

            List<String> definedConstants = findDefinedConstants(manager, navigator);
            if (definedConstants.Count > 0)
            {
                String xpathQuery = "//base:if[@diff:status]/base:condition/base:expr[base:name='" + definedConstants.ElementAt(0) + "' ";
                for (int i = 2; i < definedConstants.Count; i += 2)
                    xpathQuery += "or base:name='" + definedConstants.ElementAt(i) + "' ";
                xpathQuery += "]/base:name[@diff:status='added' or @diff:status='modified']";

                // Najdenie porovnani voci zadefinovanej konstante
                nodes = navigator.Select(xpathQuery, manager);
                while(nodes.MoveNext())
                {
                    XPathNavigator nodesNavigator = nodes.Current;
                    writeActionSpecialCaseConst(manager, nodesNavigator,definedConstants);
                }
            }
            
            
        }
    }
}
