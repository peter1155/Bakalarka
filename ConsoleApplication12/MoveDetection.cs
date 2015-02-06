using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace ConsoleApplication12
{
    class MoveDetection
    {
        private void detectCallFunctionMoves(XmlDocument doc)
        {
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je added
            XmlNodeList nodesAdded = doc.SelectNodes("//action[name='function_call' and diff_type='added']");
            
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je deleted
            XmlNodeList nodesDeleted = doc.SelectNodes("//action[name='function_call' and diff_type='deleted']");
           
            List<String> idList1 = new List<String>();
            List<String> idList2 = new List<String>();

            foreach(XmlNode nodeAdded in nodesAdded)
                foreach(XmlNode nodeDeleted in nodesDeleted)
                {
                    var id1 =  nodeAdded.Attributes.GetNamedItem("id"); 
                    var id2 =  nodeDeleted.Attributes.GetNamedItem("id"); 

                    // Ak sa indexy elementov rovnaju jedna sa o move preto je potrebne jeden s elementov vymazat
                    // a druhemu zmenit diff_type
                    if(id1.Value == id2.Value)
                    {
                        var parent = nodeDeleted.ParentNode;
                        parent.RemoveChild(nodeDeleted);
                        
                        // Pre kazdy identifikovany element zmen diff_type na hodnotu move
                        foreach(XmlNode node in nodeAdded.ChildNodes)
                        {
                            if(node.Name == "diff_type")
                            {
                                node.InnerText = "moved";
                            }
                        }
                    }
                }

            // Vymaz idecka z vysledneho suboru pretoze su dost metuce
            foreach(XmlNode nodeAdded in nodesAdded)
                    nodeAdded.Attributes.RemoveAll();
            foreach(XmlNode nodeDeleted in nodesDeleted)
                    nodeDeleted.Attributes.RemoveAll();
                
        }

        // Detekuje presunute elementy postProcessingom suboru so zaznamenanymi cinnostami
        public void detectMoves()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("RecordedActions.xml");
            
            // Detekujem presuny elementov call function

            detectCallFunctionMoves(doc);

            doc.Save("RecordedActions.xml");
            

        }

    }
}
