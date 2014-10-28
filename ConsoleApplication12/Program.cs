using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApplication12
{
    class Program
    {

        /*public void GenerateDiffGram(string originalSource, string changedSource,
                                    XmlWriter diffGramWriter)
        {
            XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder |
                                             XmlDiffOptions.IgnoreNamespaces |
                                             XmlDiffOptions.IgnorePrefixes);
            XmlReader sourceReader = new XmlReader(originalSource);
            XmlReader changedReader = new XmlReader(changedSource);
            bool bIdentical = xmldiff.Compare(;
            diffGramWriter.Close();
        }*/

        static void Main(string[] args)
        {
            ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();
            String my_string = my_runner.GenerateSrcMLFromString("#include <stdio.h> int main(", ABB.SrcML.Language.C);
            System.Console.WriteLine(my_string);
            System.Console.ReadLine();
            my_runner.GenerateSrcMLFromFile("C:\\Users\\peto\\Documents\\Visual Studio 2013\\Projects\\ConsoleApplication11\\my_program10.c", "my_program10.xml", ABB.SrcML.Language.C);
        }
    }
}
