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
    class OutputChangeActivity
    {
        // Najde funkciu v ktorej sa nachadza zmena vo volani funkcie printf
        // vytvori prislusny element 
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

        // Vracia zoznam argumentov volania funkcie printf s id = id 
        // v zdrojovom subore s nazvom fileName
        private String findInSource(String id, String fileName)
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

            String temp = "//base:call[base:name='printf' and @id='" + id + "']/base:argument_list/base:argument";
            XPathNodeIterator nodes = navigator.Select(temp, manager);
            int counter = 0;
            temp = "";
            while (nodes.MoveNext())
            {
                if (counter > 1)
                    temp += ",";
                temp += nodes.Current.Value;
                // Ak sa jedna o prvy argument oddel ho dvoma tildami
                // Tak ho bude mozne jednoduchsie vyparsovat kedze v prvom
                // argumente sa moze vyskytovat ciarka
                if (counter == 0)
                    temp += "~~";
                counter++;
            }
            return temp;
        }

        private String removeWhitespace(String str)
        {
            str = str.Replace(" ", "");
            str = str.Replace("\t", "");
            str = str.Replace("\n", "");
            return str;
        }

        // Zapis identifikovanu zmenu funkcie printf do vystupneho xml suboru
        public void writeActionPrint(XPathNavigator navigator)
        {
            // Ziskaj nazov funkcie v ktorej to je vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zistujem poziciu funkcie printf
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            // Ziskam id funkcie printf
            String id = tempNavigator2.GetAttribute("id", "");
            
            // Ziska zoznam argumentov funkcie printf
            String parametersBefore = findInSource(id, "source_data1.xml");
            String parametersAfter = findInSource(id, "source_data2.xml");
            String literalBefore;
            String literalAfter;

            string[] delimiters = { "~~" };

            // Ziskavame hodnotu vstupnych arg. pred zmenou
            string[] beforeAfterValues = parametersBefore.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
            literalBefore = beforeAfterValues[0];
            parametersBefore = beforeAfterValues[1];

            // Ziskavame hodnotu vstupnych arg. po zmene
            beforeAfterValues = parametersAfter.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
            literalAfter = beforeAfterValues[0];
            parametersAfter = beforeAfterValues[1];

            //////////////////////////////////////////////////////// Najdi poziciu ///////////////////////////////////////////////

            // Ziskam pristup k detom elementu call
            XPathNodeIterator callChildren =
                tempNavigator2.SelectChildren(XPathNodeType.Element);

            // Ziskam pristup k name kde sa nachadza prislusny attribut
            while (callChildren.MoveNext() && callChildren.Current.Name != "name") ;

            XPathNavigator callNameNav = callChildren.Current;
            String line = callNameNav.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = callNameNav.GetAttribute("column", "http://www.sdml.info/srcML/position");

            //////////////////////////////////////////////////////// Najdi poziciu ///////////////////////////////////////////////

            // Urcuje typ zmeny 
            String type;


            if (String.Compare(literalBefore, literalAfter) != 0)
            {
                if (String.Compare(parametersBefore, parametersAfter) != 0)
                {
                    type = "literal+parameter";
                }
                else
                {
                    type = "literal";
                }
            }
            else
            {
                type = "parameter";
            }

            String tempParametersBefore = parametersBefore;
            String tempParametersAfter = parametersAfter;

            tempParametersAfter = removeWhitespace(tempParametersAfter);
            tempParametersBefore = removeWhitespace(tempParametersBefore);

            if (literalBefore == literalAfter && tempParametersBefore == tempParametersAfter)
                return;

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "output_change"),
                    new XElement("type", type),
                    functionElement,
                    new XElement("literal_before", literalBefore),
                    new XElement("literal_after", literalAfter),
                    new XElement("parameters_before", parametersBefore),
                    new XElement("parameters_after", parametersAfter),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Urobim dopyt nad difference XML dokumentom a vyhladam volania funkcie printf, kde nastala nejaka zmena
        public void findDifferenceInOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Najdi volania funkcie printf kde funkcia printf nebola pridana ani zmazana ale 
            // zoznam jej parametrov sa zmenil
            XPathNodeIterator nodes = navigator.Select("//base:call[not(base:name[@diff:status]) and base:name='printf' and  @diff:status='below' and base:argument_list[" +
            "base:argument/base:expr[lit:literal/@diff:status or base:name/@diff:status or base:call/@diff:status]]]"
            + " | //base:call[not(base:name[@diff:status]) and base:name='printf' and /base:argument_list[@similarity != '1']]", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                writeActionPrint(currentNode);
            }
        }
    }
}