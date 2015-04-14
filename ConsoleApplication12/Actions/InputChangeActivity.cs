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


namespace ConsoleApplication12.Actions
{
    class InputChangeActivity
    {
        
        private String FindInSource(String id, String fileName)
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

        private List<String> ManualParse(XPathNavigator navigator)
        {
            String parameters_before = null;
            String parameters_after = null;
            String[] beforeAfterValues = null;
            char[] delimiters = { '~' };
            // Select arguments from argument list
            XPathNodeIterator call_children = navigator.SelectChildren(XPathNodeType.Element);
            XPathNavigator call_child = null;

            while (call_children.MoveNext())
            {
                if (String.Compare(call_children.Current.Name, "argument_list") == 0)
                {
                    call_child = call_children.Current;
                    break;
                }
            }

            XPathNodeIterator arguments = call_child.SelectChildren(XPathNodeType.Element);

            // Prechadzame vsetkymi argumentmi funkcie 
            int in_after_counter = 0;
            int out_after_counter = 0;

            int in_before_counter = 0;
            int out_before_counter = 0;


            while (arguments.MoveNext())
            {
                XPathNodeIterator expresionIterator = arguments.Current.SelectChildren(XPathNodeType.Element);
                expresionIterator.MoveNext();
                XPathNodeIterator parameterIterator = expresionIterator.Current.SelectChildren(XPathNodeType.Element);

                in_after_counter = 0;
                in_before_counter = 0;

                while (parameterIterator.MoveNext())
                {
                    XPathNavigator parameter = parameterIterator.Current;
                    String temp = parameter.GetAttribute("status", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");
                    String tempValue = parameter.Value;

                    if (String.Compare(temp, "modified") == 0)
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += "~~";
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += "~~";

                        out_after_counter++;
                        in_after_counter++;

                        out_before_counter++;
                        in_before_counter++;

                        beforeAfterValues = tempValue.Split(delimiters);
                        parameters_before += beforeAfterValues[0];
                        parameters_after += beforeAfterValues[1];
                    }
                    else if (String.Compare(temp, "added") == 0)
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += "~~";
                        out_after_counter++;
                        in_after_counter++;
                        parameters_after += tempValue;
                    }
                    else if (String.Compare(temp, "removed") == 0)
                    {
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += "~~";
                        out_before_counter++;
                        in_before_counter++;
                        parameters_before += tempValue;
                    }
                    else if (String.Compare(temp, "below") == 0)
                    {

                        // parmeterObject.type.Add("removed");
                    }
                    else
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += "~~";
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += "~~";

                        out_after_counter++;
                        in_after_counter++;

                        out_before_counter++;
                        in_before_counter++;

                        parameters_before += tempValue;
                        parameters_after += tempValue;
                    }
                }

            }

            List<String> list = new List<String>();
            list.Add(parameters_before);
            list.Add(parameters_after);
            return list;
        }
        
        private String RemoveWhitespace(String str)
        {
            str = str.Replace(" ", "");
            str = str.Replace("\t", "");
            str = str.Replace("\n", "");
            return str;
        }

        public void WriteActionScan(XPathNavigator navigator)
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
            String temp_id = tempNavigator2.GetAttribute("temp_id", "");
            String parametersBefore = FindInSource(id,"source_data1.xml");
            String parametersAfter = FindInSource(id,"source_data2.xml");
            String literalBefore;
            String literalAfter;
            string[] beforeAfterValues = null;

            if (String.Compare(id, "") == 0)
            {
                var list = ManualParse(tempNavigator2.Clone());
                //manualParse2(tempNavigator2.Clone(),temp_id,"source_data2.xml", "source_data1.xml");
                
                parametersBefore = list.ElementAt(0);
                parametersAfter = list.ElementAt(1);
               
                
                string[] delimiters = { "~~" };

                // Ziskavame hodnotu vstupnych atr. pred zmenou
                beforeAfterValues = parametersBefore.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
                literalBefore = beforeAfterValues[0];
                parametersBefore.Replace("~~", ",");
                parametersBefore = parametersBefore.Substring(literalBefore.Length + 1);

                // Ziskavame hodnotu vstupnych atr. po zmene
                beforeAfterValues = parametersAfter.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
                literalAfter = beforeAfterValues[0];
                parametersAfter.Replace("~~", ",");
                parametersAfter = parametersAfter.Substring(literalAfter.Length + 1);
            }
            else
            {
                string[] delimiters = { "~~" };

                // Ziskavame hodnotu vstupnych atr. pred zmenou
                beforeAfterValues = parametersBefore.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
                literalBefore = beforeAfterValues[0];
                parametersBefore = beforeAfterValues[1];

                // Ziskavame hodnotu vstupnych atr. po zmene
                beforeAfterValues = parametersAfter.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
                literalAfter = beforeAfterValues[0];
                parametersAfter = beforeAfterValues[1];
            }
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

            XElement functionElement = null;
            String funcName = functionNameNav.Value;

            // Osetruje pripad ze diff zaznamena meno predchadzajucej funkcie ako zmazane a sucasnej ako pridane ....
            List<String> callNames = new List<string>();
            callNames.Add(funcName);
            while (function_childeren.MoveNext() && function_childeren.Current.Name == "name")
            {
                callNames.Add(function_childeren.Current.Value);
            }

            // Ak doslo k modifikacii nazvu funkcie treba to osetrit
            if (funcName.Contains("~"))
            {
                char[] del = { '~' };
                beforeAfterValues = funcName.Split(del);
                functionElement = new XElement("function_name",
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]));
            }
            else if (callNames.Count > 1)
            {
                functionElement = new XElement("function_name",
                    new XElement("before", callNames.ElementAt(1)),
                    new XElement("after", callNames.ElementAt(0)));
            }
            else
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcName),
                    new XElement("after", funcName));
            }


            String tempParametersBefore = parametersBefore;
            String tempParametersAfter = parametersAfter;

            tempParametersAfter = RemoveWhitespace(tempParametersAfter);
            tempParametersBefore = RemoveWhitespace(tempParametersBefore);

            if (literalBefore == literalAfter && tempParametersBefore == tempParametersAfter)
                return;
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            
            XElement my_element = new XElement("action",
                    new XElement("name", "input_change"),
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

        // Urobim dopyt nad difference XML dokumentom a vyhladam volania funkcie scanf, kde nastala nejaka zmena
        public void FindDifferenceInInput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:call[not(base:name[@diff:status]) and base:name='scanf' and  @diff:status='below']/base:argument_list[" +
            "base:argument/base:expr[lit:literal/@diff:status or base:name/@diff:status or base:call/@diff:status]]"
            + " | //base:call[not(base:name[@diff:status]) and base:name='scanf' and  @diff:status='below' ]/base:argument_list[@similarity!='1'] ", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                WriteActionScan(currentNode);
            }
        }
    }
}