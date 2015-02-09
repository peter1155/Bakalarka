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
    class AddFunctionActivity
    {
        // Zapise prislusnu akciu do vystupneho xml suboru
        public void writeActionScan(XPathNavigator navigator)
        {
            // Sluzi na zaznamenanie cisla riadku
            String line = null;

            // Sluzi na zaznamenanie cisla stlpca
            String column = null;

            // Sluzi na zaznamenanie typu pridanej funkcie
            String type = null;

            // Sluzi na zaznamenanie mena pridanej funkcie
            String name = null;

            // Sluzi na zaznamenie zoznamu parametrov
            XElement parameter_list2 = new XElement("parameter_list");

            // Ziska pristup k detom elementu function
            XPathNodeIterator function_children = navigator.SelectChildren(XPathNodeType.Element);
            
            while(function_children.MoveNext())
            {
                XPathNavigator child = function_children.Current;
                
                // Ak je nazov aktualneho elementu type prirad hodnotu do type
                if(String.Compare(child.Name,"type") == 0)
                {
                    type = child.Value;
                }

                // Ak je nazov aktualneho elementu name prirad hodnotu do name a ziskaj poziciu z atributov line a column
                else if(String.Compare(child.Name,"name") == 0)
                {
                    name = child.Value;
                    line = child.GetAttribute("line", "http://www.sdml.info/srcML/position");
                    column = child.GetAttribute("column", "http://www.sdml.info/srcML/position");
                }
                // Ak je nazov aktualneho elementu parameter_list vytvor zoznam parametrov a pridaj ho do prislusneho elementu
                else if(String.Compare(child.Name,"parameter_list") == 0)
                {
                    XPathNodeIterator param_list = child.SelectChildren(XPathNodeType.Element);
                    // Prechadzam jednotlivymi parametrami 
                    while (param_list.MoveNext())
                    {
                        XPathNavigator param = param_list.Current;

                        // Ziskam pristup k detom param teda declaretion
                        XPathNodeIterator param_children = param.SelectChildren(XPathNodeType.Element);
                        param_children.MoveNext();
                        XPathNavigator param_child = param_children.Current;

                        // Nakoniec ziskam pristup k detom declaration 
                        XElement parameter = new XElement("parameter");
                        XPathNodeIterator decl_children = param_child.SelectChildren(XPathNodeType.Element);
                       
                        while (decl_children.MoveNext())
                        {
                            XPathNavigator decl_child = decl_children.Current;
                            if (String.Compare(decl_child.Name, "type") == 0)
                                parameter.Add(new XElement("type", decl_child.Value));
                            if (String.Compare(decl_child.Name, "name") == 0)
                            {
                                // Meno moze obsahovat indexy preto treba prechadzat postupne
                                XPathNodeIterator name_children = decl_child.SelectChildren(XPathNodeType.Element);
                                XElement nameElement = new XElement("name");
                                if (name_children.Count > 0)
                                {
                                    while (name_children.MoveNext())
                                    {
                                        XPathNavigator name_child = name_children.Current;
                                        if (String.Compare(name_child.Name, "name") == 0)
                                        {
                                            nameElement.Add(new XElement("name", name_child.Value));
                                        }
                                        if (String.Compare(name_child.Name, "index") == 0)
                                        {
                                            nameElement.Add(new XElement("index", name_child.Value));
                                        }
                                    }
                                    parameter.Add(nameElement);
                                }
                                else
                                    parameter.Add(new XElement("name",decl_child.TypedValue));
                            }
                        }
                        parameter_list2.Add(parameter);
                    }
                }
            }
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            
            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "function_added"),
                    new XElement("type", type),
                    new XElement("function_name", name),
                    parameter_list2,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Urobi dopyt nad difference XML dokumentom a vyhlada pridane funkcie ak sa nejake najdu
        // zaznamena prislusnu akciu do vystupneho xml suboru
        public void findAddedFunctions(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Najde pridane funkcie
            XPathNodeIterator nodes = navigator.Select("//base:function[@diff:status='added']", manager);

            // Prechadza zoznamom pridanych funkcii a zaznamenava ich do vystupneho xml suboru
            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                writeActionScan(currentNode);
            }
        }
    }
}
