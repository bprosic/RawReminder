using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Terminal = System.Console;

namespace RawReminder
{
    /// <summary>
    /// Class for logger
    /// Set console properties
    /// Help method for user
    /// </summary>
    class HelpFunctions
    {
        public HelpFunctions()
        {
        }
        #region Logger - log4net. Is this good technique - global var?
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Determine Window Console Width
        public static int WindowWidth { get; set; }

        static public void SetWindowWidth(int windowWidth, int windowHeight)
        {
            int origWidth = Terminal.WindowWidth;
            int origHeight = Terminal.WindowHeight;

            if (origWidth <= 100)
                origWidth = windowWidth;
            if (origHeight <= 40)
                origHeight = windowHeight;


            Terminal.SetWindowSize(origWidth, origHeight);
        }
        #endregion

        #region Show help in console
        static public void ShowHelp()
        {
            var helpInfo = new Dictionary<string, string>();
            helpInfo.Add("set", "insert new reminder with some parameters");
            helpInfo.Add("update", "update reminder based on row Id");
            helpInfo.Add("delete", "delete reminder based on row Id");
            helpInfo.Add("cls/clear", "clear screen");
            helpInfo.Add("showR", "show reminders table in DB");
            helpInfo.Add("showH", "show history reminders table in DB");
            helpInfo.Add("status", "gets the status");

            Terminal.WriteLine(helpInfo.ToStringTable(new[] { "Command", "Description" }, dd => dd.Key, dd => dd.Value));
        }
        #endregion

        #region Get Properties Names of a Class in List
        // To get all properties name of a class (pass class as parameter)
        [Obsolete("Please, use MemberHelper class")]
        static public List<string> GetPropertiesNamesOfAClass(Type someClassName)
        {
            object someObj = Activator.CreateInstance(someClassName);
            var type = someObj.GetType();
            var propList = new List<string>();

            foreach (PropertyInfo prop in type.GetProperties())
                propList.Add(prop.Name);

            return propList;
        } 
        #endregion
    }

    #region Get Propertie Name of a Class
    //
    // Member helper is to get attributes names in a class
    // 
    public class MemberHelper<T>
    {
        public string GetName<U>(Expression<Func<T, U>> expression)
        {
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (memberExpression != null)
                return memberExpression.Member.Name;
            throw new InvalidOperationException("Member expression expected");
        }
    }
    #endregion

    
}
