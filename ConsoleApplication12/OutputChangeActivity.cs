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
        public class OutputObject
        {
            public OutputObject()
            {
                parametersBefore = new List<string>();
                parametersAfter = new List<string>();
            }

            public String literalBefore {get;set;}
            public String literalAfter{get;set;}
            public List<String> parametersBefore{get;set;}
            public List<String> parametersAfter {get;set;}
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

        // inParameter is list of arguments and namespace manager
        private OutputObject prepareChangeObject(XPathNavigator navigator, XmlNamespaceManager manager) 
        {
            var parameterListModified = new List<ParameterObject>();
            String diffType = "";

            var parameterListBefore = new List<String>();
            var parameterListAfter = new List<String>();
           

            String literalChange = "";

            // Select arguments from argument list
            XPathNodeIterator arguments = navigator.SelectChildren(XPathNodeType.Element);

            while (arguments.MoveNext())
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
                        parmeterObject.type.Add("removed");
                    }
                    else
                    {
                        parmeterObject.type.Add("same");
                    }

                    if (String.Compare(parameter.Name, "name") == 0 || String.Compare(parameter.Name, "op:operator") == 0)              
                        parmeterObject.content.Add(parameter.Value);
                    else
                    {
                        literalChange = parameter.Value;
                        diffType = temp;
                    }
                }
                parameterListModified.Add(parmeterObject);
            }

            char[] delimiters = { '~' };
            string[] beforeAfterValues = literalChange.Split(delimiters); // Rozsekam zmenu na casti

            OutputObject outputObject = new OutputObject();
            
            if(String.Compare(diffType,"") != 0)
            {
                outputObject.literalBefore = beforeAfterValues[0];
                outputObject.literalAfter = beforeAfterValues[1];
            }
            else
            {
                outputObject.literalBefore = beforeAfterValues[0];
                outputObject.literalAfter = beforeAfterValues[0];
            }
            outputObject.diffType = diffType;

            for (int j = 0; j < parameterListModified.Count; j++)
            {
                // toto este prerobit
                if (j > 1)
                {
                    outputObject.parametersBefore.Add(",");
                    outputObject.parametersAfter.Add(",");
                }
                for (int i = 0; i < parameterListModified.ElementAt(j).content.Count; i++)
                {
                    switch (parameterListModified.ElementAt(j).type.ElementAt(i))
                    {
                        case "modified":
                            {
                                string[] beforeAfter = parameterListModified.ElementAt(j).content.ElementAt(i).Split(delimiters);
                                outputObject.parametersBefore.Add(beforeAfter[0]);
                                outputObject.parametersAfter.Add(beforeAfter[1]);
                                break;
                            }
                        case "added":
                            {
                                outputObject.parametersAfter.Add(parameterListModified.
                                    ElementAt(j).content.ElementAt(i));
                                break;
                            }
                        case "removed":
                            {
                                outputObject.parametersBefore.Add(parameterListModified.
                                    ElementAt(j).content.ElementAt(i));
                                break;
                            }
                        case "same":
                            {
                                outputObject.parametersBefore.Add(parameterListModified.
                                    ElementAt(j).content.ElementAt(i));
                                outputObject.parametersAfter.Add(parameterListModified.
                                    ElementAt(j).content.ElementAt(i));
                                break;
                            }
                    }
                }
            }
            return outputObject;
        }

        public void writeActionPrintout(XPathNavigator navigator, OutputObject outputObject)
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

            String type;

            if (String.Compare(literalBefore, literalAfter) != 0)
            {
                if (String.Compare(parmetersBefore, parametersAfter) != 0)
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

            // Zistujem poziciu funkcie printf
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

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("acion",
                    new XElement("name", "OutputChange"),
                    new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("function", functionNameNav.Value),
                    new XElement("LiteralBefore", literalBefore),
                    new XElement("LiteralAfter", literalAfter),
                    new XElement("ParametersBefore", parmetersBefore),
                    new XElement("ParametersAfter", parametersAfter),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void findDifferenceInOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:call[base:name='printf']/base:argument_list[" +
            "base:argument/base:expr[lit:literal/@diff:status or base:name/@diff:status]]"
            , manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                OutputObject outputObject = prepareChangeObject(currentNode, manager);
                writeActionPrintout(nodes.Current,outputObject);   
            }
            System.Console.ReadLine();
        }
    }
}
