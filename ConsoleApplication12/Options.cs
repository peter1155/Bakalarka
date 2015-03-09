using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

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
            Hide, Show
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
        public static void LoadProgramConfiguration() 
        {
            TaskPath = System.Configuration.ConfigurationManager.AppSettings["taskPath"];
            if(!Directory.Exists(TaskPath))
                throw new System.ArgumentException("Error ! Zadana cesta neexistuje: "+TaskPath);
            
            StartStudent = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["studentStart"]);

            if (StartStudent < 0 || StartStudent > 1000)
                throw new System.ArgumentException("Error ! Cislo studenta (StartStudent) musi byt v rozsahu <0,1000>");

            EndStudent = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["studentEnd"]);

            if (EndStudent < 1 || EndStudent > 1000)
                throw new System.ArgumentException("Error ! Cislo studenta (EndStudent) musi byt v rozsahu <1,1000>");

            StartAttempt = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["attemptStart"]);
            if (StartAttempt < 0 || StartAttempt > 30)
                throw new System.ArgumentException("Error ! Cislo pokusu (StartAttempt) musi byt v rozsahu <0,30>");

            EndAttempt = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["attemptEnd"]);
            if (EndAttempt < 1 || EndAttempt > 30)
                throw new System.ArgumentException("Error ! Cislo pokusu (EndAttempt) musi byt v rozsahu <1,30>");
            
            Method = (Methods)Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["method"]);
            if(Method!= Methods.Complex && Method != Methods.Fast)
                throw new System.ArgumentException("Error ! Nespravna volba metoda moze nadobudat iba hodnoty 0,1");
            
            Time = (ShowTime)Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["timeShow"]);
            if(Time!= ShowTime.Hide && Time!= ShowTime.Show)
                throw new System.ArgumentException("Error ! Nespravna volba cas mozno iba ukazat alebo schovat (0,1)");
        }      
    }
}
