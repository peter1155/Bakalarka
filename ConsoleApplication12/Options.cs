using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ConsoleApplication12
{
  
    public class Options
    {

        public enum Methods
        {
            Fast, Complex
        }

        public enum ShowTime
        {
            Hide,Show
        }

        // cesta k suborom so zdrojovymi kodmi
        public static string TaskPath { get; private set; }

        // cislo studenta od ktoreho sa ma zacat 
        public static int StartStudent { get; private set; }

        // cislo studenta pri ktorom sa ma skoncit
        public static int EndStudent { get; private set; }

        // cislo pokusu od ktoreho sa ma zacat
        public static int StartAttempt { get; private set; }

        // cislo pokusu pri ktorom sa ma skoncit
        public static int EndAttempt { get; private set; }

        // metoda ktora sa ma pouzit
        public static Methods Method { get; private set; }

        // metoda ktora sa ma pouzit
        public static ShowTime Time { get; private set; }
        
        // Metoda nacita konfiguraciu s konfiguracneho suboru
        public static void loadProgramConfiguration()
        {
            TaskPath = System.Configuration.ConfigurationManager.AppSettings["taskPath"];
            StartStudent = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["studentStart"]);
            EndStudent = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["studentEnd"]);
            StartAttempt = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["AttemptStart"]);
            EndAttempt = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["AttemptEnd"]);
            Method = (Methods)Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["method"]);
            Time = (ShowTime)Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["timeShow"]);
        }
    
        
        
    }
}
