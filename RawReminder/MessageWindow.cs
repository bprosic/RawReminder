using System.Windows.Forms;
using xDialog;
using Terminal = System.Console;
namespace RawReminder
{
    /// <summary>
    /// Show Message Box to the user - like window PopUp for a reminder.
    /// </summary>
    class MessageWindow
    {
        public string receiveMessage { get; set; }

        public MessageWindow(string message)
        {
                DialogResult result = MsgBox.Show(message, "Reminder", MsgBox.Buttons.OK, MsgBox.Icon.Info, MsgBox.AnimateStyle.FadeIn);
                if (result == DialogResult.OK)
                {
                    // here I will implement something when user presses ok
                }

        }
    }
}
