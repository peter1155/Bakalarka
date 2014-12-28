﻿/*using Microsoft.XmlDiffPatch;
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
    class InputChangeActivity
    {
        // Tuto triedu pouzivam na posielanie parametrov na vypis do xml recorded actions
        public class OutputObject
        {
            public OutputObject()
            {
                parametersBefore = new List<string>();
                parametersAfter = new List<string>();
            }

            public String literalBefore { get; set; }
            public String literalAfter { get; set; }
            public List<String> parametersBefore { get; set; }
            public List<String> parametersAfter { get; set; }
            public String diffType { get; set; }
        }

        public class ParameterObject
        {
            public ParameterObject()
            {
                this.content = new List<String>();
                this.type = new List<String>();
            }
            public List<String> type { get; set; }
            public List<String> content { get; set; }
        }

        private String findInSource(String line, String fileName)
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

            String temp = "//base:call[base:name='printf' and base:name/@line='" + line + "']/base:argument_list";
            XPathNodeIterator nodes = navigator.Select(temp, manager);
            while (nodes.MoveNext())
            {
                temp = nodes.Current.Value;
            }
            return temp;
        }

        private void commaSeparator(List<String> parameters)
        {
            if (parameters.Count > 0) parameters.Add(",");
        }

        public String findInSourceInput(String line, String fileName)
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

            String temp = "//base:call[base:name='scanf' and base:name/@line='" + line + "']/base:argument_list";
            XPathNodeIterator nodes = navigator.Select(temp, manager);
            while (nodes.MoveNext())
            {
                temp = nodes.Current.Value;
            }
            return temp;
        }

        // inParameter is list of arguments and namespace manager
        private OutputObject prepareInputChangeObject(XPathNavigator navigator, XmlNamespaceManager manager)
        {
            var parameterListModified = new List<ParameterObject>();
           
            // Ak viem zistit ulozim sem cislo riadka naa ktorom sa nachadza printf v prvom zdrojaku
            String line = null;
            // Tu vlozim true ak viem zistit zo zdrojaku povodne parametre printf
            Boolean findedInSource = false;

            var parameterListBefore = new List<String>();
            var parameterListAfter = new List<String>();

            String literalChange = "";

            // Select arguments from argument list
            XPathNodeIterator arguments = navigator.SelectChildren(XPathNodeType.Element);

            // Prechadzame vsetkymi argumentmi funkcie printf
            int counter = 0;
            while (arguments.MoveNext() && !findedInSource)
            {
                XPathNodeIterator expresionIterator = arguments.Current.SelectChildren(XPathNodeType.Element);
                expresionIterator.MoveNext();
                XPathNodeIterator parameterIterator = expresionIterator.Current.SelectChildren(XPathNodeType.Element);

                ParameterObject parmeterObject = new ParameterObject();

                while (parameterIterator.MoveNext())
                {
                    XPathNavigator parameter = parameterIterator.Current;
                    String temp = parameter.GetAttribute("status", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");

                    if (String.Compare(temp, "modified") == 0)
                    {
                        parmeterObject.type.Add("modified");
                    }
                    else if (String.Compare(temp, "added") == 0)
                    {
                        parmeterObject.type.Add("added");
                    }
                    else if (String.Compare(temp, "removed") == 0)
                    {
                        // Ak najdem parameter, ktory bol vymazany viem z neho ziskat cislo riadka kde sa nachadza dane volanie printf
                        line = parameter.GetAttribute("line", "http://www.sdml.info/srcML/position");
                        findedInSource = true;
                        break;
                    }
                    else if (String.Compare(temp, "below") == 0)
                    {
                        parmeterObject.type.Add("removed");
                    }
                    else
                    {
                        parmeterObject.type.Add("same");
                    }

                    if (counter != 0)
                        parmeterObject.content.Add(parameter.Value);
                    else
                        literalChange = parameter.Value;  
                }
                parameterListModified.Add(parmeterObject);
                counter++;
            }
            parameterListModified.RemoveAt(0);
            char[] delimiters = { '~' };
            string[] beforeAfterValues = literalChange.Split(delimiters); // Rozsekam zmenu na casti

            OutputObject outputObject = new OutputObject();

            if (beforeAfterValues.Length != 1)
            {
                outputObject.literalBefore = beforeAfterValues[0];
                outputObject.literalAfter = beforeAfterValues[1];
            }
            else
            {
                outputObject.literalBefore = beforeAfterValues[0];
                outputObject.literalAfter = beforeAfterValues[0];
            }
           

            if (!findedInSource)
            {
                for (int j = 0; j < parameterListModified.Count; j++)
                {
                    for (int i = 0; i < parameterListModified.ElementAt(j).content.Count; i++)
                    {
                        switch (parameterListModified.ElementAt(j).type.ElementAt(i))
                        {
                            case "modified":
                                {
                                    string[] beforeAfter = parameterListModified.ElementAt(j).content.ElementAt(i).Split(delimiters);
                                    if (i == 0 && outputObject.parametersBefore.Count > 0)
                                        commaSeparator(outputObject.parametersBefore);
                                    if (i == 0 && outputObject.parametersAfter.Count > 0)
                                        commaSeparator(outputObject.parametersAfter);
                                    outputObject.parametersBefore.Add(beforeAfter[0]);
                                    outputObject.parametersAfter.Add(beforeAfter[1]);
                                    break;
                                }
                            case "added":
                                {
                                    if (i == 0 && outputObject.parametersAfter.Count > 0)
                                        commaSeparator(outputObject.parametersAfter);
                                    outputObject.parametersAfter.Add(parameterListModified.
                                        ElementAt(j).content.ElementAt(i));
                                    break;
                                }
                            case "removed":
                                {
                                    if ((i == 0 || (i > 0 && String.Compare(parameterListModified.ElementAt(j).type.ElementAt(i),
                                        parameterListModified.ElementAt(j).type.ElementAt(i - 1)) != 0))
                                        && outputObject.parametersBefore.Count > 0)
                                        commaSeparator(outputObject.parametersBefore);
                                    outputObject.parametersBefore.Add(parameterListModified.
                                        ElementAt(j).content.ElementAt(i));
                                    break;
                                }
                            case "same":
                                {
                                    if (i == 0 && outputObject.parametersBefore.Count > 0)
                                        commaSeparator(outputObject.parametersBefore);
                                    if (i == 0 && outputObject.parametersAfter.Count > 0)
                                        commaSeparator(outputObject.parametersAfter);
                                    outputObject.parametersBefore.Add(parameterListModified.
                                        ElementAt(j).content.ElementAt(i));
                                    outputObject.parametersAfter.Add(parameterListModified.
                                        ElementAt(j).content.ElementAt(i));
                                    break;
                                }
                        }
                    }
                }
            }
            else
            {
                String temp = findInSourceInput(line, "source_data1.xml");
                temp = temp.Substring(outputObject.literalAfter.Length + 2); // +2 pretoze (,
                temp = temp.Substring(0, temp.Length - 1);
                outputObject.parametersBefore.Add(temp);
            }
            return outputObject;
        }

        public void writeActionScan(XPathNavigator navigator, OutputObject outputObject)
        {
            String literalBefore = outputObject.literalBefore;
            String literalAfter = outputObject.literalAfter;
            String parmetersBefore = "";
            String parametersAfter = "";
            String diffType = outputObject.diffType;

            for (int i = 0; i < outputObject.parametersBefore.Count; i++)
            {
                parmetersBefore += outputObject.parametersBefore.ElementAt(i);
            }

            for (int i = 0; i < outputObject.parametersAfter.Count; i++)
            {
                parametersAfter += outputObject.parametersAfter.ElementAt(i);
            }

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

            // Zistujem poziciu funkcie scanf
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            // Ziskam pristup k detom elementu call
            XPathNodeIterator callChildren =
                tempNavigator2.SelectChildren(XPathNodeType.Element);

            // Ziskam pristup k name kde sa nachadza prislusny attribut
            while (callChildren.MoveNext() && callChildren.Current.Name != "name") ;

            XPathNavigator callNameNav = callChildren.Current;
            String line = callNameNav.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = callNameNav.GetAttribute("column", "http://www.sdml.info/srcML/position");

            String directParametersAfter = findInSourceInput(line, "source_data2.xml");

            directParametersAfter = directParametersAfter.Substring(literalAfter.Length + 2);
            directParametersAfter = directParametersAfter.Substring(0, directParametersAfter.Length - 1);

            String type;

            if (String.Compare(literalBefore, literalAfter) != 0)
            {
                if (String.Compare(parmetersBefore, directParametersAfter) != 0)
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

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("acion",
                    new XElement("name", "InputChange"),
                //new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("LiteralBefore", literalBefore),
                    new XElement("LiteralAfter", literalAfter),
                    new XElement("ParametersBefore", parmetersBefore),
                    new XElement("ParametersAfter", directParametersAfter),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Urobim dopyt nad difference XML dokumentom a vyhladam volania funkcie printf, kde nastala nejaka zmena
        public void findDifferenceInInput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:call[base:name='scanf' and  @diff:status='below']/base:argument_list[" +
            "base:argument/base:expr[lit:literal/@diff:status or base:name/@diff:status or base:call/@diff:status]]"
            , manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                OutputObject outputObject = prepareInputChangeObject(currentNode, manager);
                writeActionScan(nodes.Current, outputObject);
            }
        }
    }
}*/

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
    class InputChangeActivity
    {
        
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

            String temp = "//base:call[base:name='scanf' and @id='" + id + "']/base:argument_list/base:argument";
            XPathNodeIterator nodes = navigator.Select(temp, manager);
            int counter = 0;
            temp = "";
            while (nodes.MoveNext())
            {
                if (counter > 1)
                    temp += ",";
                temp += nodes.Current.Value;
                if (counter == 0)
                    temp += "~~";
                counter++;
            }
            return temp;
        }

        public void writeActionScan(XPathNavigator navigator)
        {
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

            // Zistujem poziciu funkcie scanf
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            // Ziskam id funkcie scanf
            String id = tempNavigator2.GetAttribute("id", "");
            String parametersBefore = findInSource(id,"source_data1.xml");
            String parametersAfter = findInSource(id,"source_data2.xml");
            String literalBefore;
            String literalAfter;

            string[] delimiters = { "~~" };
            
            // Ziskavame hodnotu vstupnych atr. pred zmenou
            string[] beforeAfterValues = parametersBefore.Split(delimiters,StringSplitOptions.None); // Rozsekam zmenu na casti
            literalBefore = beforeAfterValues[0];
            parametersBefore = beforeAfterValues[1];

            // Ziskavame hodnotu vstupnych atr. po zmene
            beforeAfterValues = parametersAfter.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
            literalAfter = beforeAfterValues[0];
            parametersAfter = beforeAfterValues[1];

            // Ziskam pristup k detom elementu call
            XPathNodeIterator callChildren =
                tempNavigator2.SelectChildren(XPathNodeType.Element);

            // Ziskam pristup k name kde sa nachadza prislusny attribut
            while (callChildren.MoveNext() && callChildren.Current.Name != "name") ;

            XPathNavigator callNameNav = callChildren.Current;
            String line = callNameNav.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = callNameNav.GetAttribute("column", "http://www.sdml.info/srcML/position");
            
         
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
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            
            XElement my_element = new XElement("acion",
                    new XElement("name", "InputChange"),
                //new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("LiteralBefore", literalBefore),
                    new XElement("LiteralAfter", literalAfter),
                    new XElement("ParametersBefore", parametersBefore),
                    new XElement("ParametersAfter", parametersAfter),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Urobim dopyt nad difference XML dokumentom a vyhladam volania funkcie printf, kde nastala nejaka zmena
        public void findDifferenceInInput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:call[base:name='scanf' and  @diff:status='below']/base:argument_list[" +
            "base:argument/base:expr[lit:literal/@diff:status or base:name/@diff:status or base:call/@diff:status]]"
            , manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                writeActionScan(currentNode);
            }
        }
    }
}