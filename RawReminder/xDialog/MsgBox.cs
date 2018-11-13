using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace xDialog
{
    // https://www.codeproject.com/Articles/17253/A-Custom-Message-Box
    // removed what i dont need

    partial class MsgBox : Form
    {
        const int CS_DROPSHADOW = 0x00020000;
        static MsgBox MsgBoxx;
        Panel PlHeader = new Panel();
        Panel PlFooter = new Panel();
        Panel PlIcon = new Panel();
        PictureBox PictureIcon = new PictureBox();
        FlowLayoutPanel FlowLayoutPanel = new FlowLayoutPanel();
        Label LblTitle;
        Label LblMessage;
        List<Button> ButtonCollection = new List<Button>();
        static DialogResult ButtonResult = new DialogResult();
        static Timer SomeTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool MessageBeep(uint type);

        // make window dragable
        bool isMouseDown { get; set; }
        Point FormLastLocation { get; set; } 

        private MsgBox()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(3);
            this.Width = 400;
            

            LblTitle = new Label();
            LblTitle.ForeColor = Color.White;
            LblTitle.Font = new System.Drawing.Font("Segoe UI", 18);
            LblTitle.Dock = DockStyle.Top;
            LblTitle.Height = 50;

            LblMessage = new Label();
            LblMessage.ForeColor = Color.White;
            LblMessage.Font = new System.Drawing.Font("Segoe UI", 10);
            LblMessage.Dock = DockStyle.Fill;

            FlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
            FlowLayoutPanel.Dock = DockStyle.Fill;

            PlHeader.Dock = DockStyle.Fill;
            PlHeader.Padding = new Padding(20);
            PlHeader.Controls.Add(LblMessage);
            PlHeader.Controls.Add(LblTitle);

            PlFooter.Dock = DockStyle.Bottom;
            PlFooter.Padding = new Padding(20);
            PlFooter.BackColor = Color.FromArgb(37, 37, 38);
            PlFooter.Height = 80;
            PlFooter.Controls.Add(FlowLayoutPanel);

            PictureIcon.Width = 32;
            PictureIcon.Height = 32;
            PictureIcon.Location = new Point(30, 50);

            PlIcon.Dock = DockStyle.Left;
            PlIcon.Padding = new Padding(20);
            PlIcon.Width = 70;
            PlIcon.Controls.Add(PictureIcon);

            this.Controls.Add(PlHeader);
            this.Controls.Add(PlIcon);
            this.Controls.Add(PlFooter);
            
            this.PlHeader.MouseDown += new MouseEventHandler(MessageBoxForm_MouseDown);
            this.PlHeader.MouseUp += new MouseEventHandler(MessageBoxForm_MouseUp);
            this.PlHeader.MouseMove += new MouseEventHandler(MessageBoxForm_MouseMove);

        }

        public static void Show(string message)
        {
            MsgBoxx = new MsgBox();
            MsgBoxx.LblMessage.Text = message;
            MsgBoxx.ShowDialog();
            MessageBeep(0);
        }

        void MessageBoxForm_MouseDown(object s, MouseEventArgs e)
        {
            isMouseDown = true;
            FormLastLocation = e.Location;
        }

        void MessageBoxForm_MouseMove(object s, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - FormLastLocation.X) + e.X, (this.Location.Y - FormLastLocation.Y) + e.Y);

                this.Update();
            }
        }

        void MessageBoxForm_MouseUp(object s, MouseEventArgs e)
        {
            isMouseDown = false;
        }



        public static void Show(string message, string title)
        {
            MsgBoxx = new MsgBox();
            MsgBoxx.LblMessage.Text = message;
            MsgBoxx.LblTitle.Text = title;
            MsgBoxx.Size = MsgBox.MessageSize(message);
            MsgBoxx.ShowDialog();
            MessageBeep(0);
        }

        public static DialogResult Show(string message, string title, Buttons buttons)
        {
            MsgBoxx = new MsgBox();
            MsgBoxx.LblMessage.Text = message;
            MsgBoxx.LblTitle.Text = title;
            MsgBoxx.PlIcon.Hide();

            MsgBox.InitButtons(buttons);

            MsgBoxx.Size = MsgBox.MessageSize(message);
            MsgBoxx.ShowDialog();
            MessageBeep(0);
            return ButtonResult;
        }

        public static DialogResult Show(string message, string title, Buttons buttons, Icon icon)
        {
            MsgBoxx = new MsgBox();
            MsgBoxx.LblMessage.Text = message;
            MsgBoxx.LblTitle.Text = title;

            MsgBox.InitButtons(buttons);
            MsgBox.InitIcon(icon);

            MsgBoxx.Size = MsgBox.MessageSize(message);
            MsgBoxx.ShowDialog();
            MessageBeep(0);
            return ButtonResult;
        }

        public static DialogResult Show(string message, string title, Buttons buttons, Icon icon, AnimateStyle style)
        {
            MsgBoxx = new MsgBox();
            MsgBoxx.LblMessage.Text = message;
            MsgBoxx.LblTitle.Text = title;
            MsgBoxx.Height = 0;

            MsgBox.InitButtons(buttons);
            MsgBox.InitIcon(icon);

            SomeTimer = new Timer();
            Size formSize = MsgBox.MessageSize(message);

            switch (style)
            {
                case MsgBox.AnimateStyle.SlideDown:
                    MsgBoxx.Size = new Size(formSize.Width, 0);
                    SomeTimer.Interval = 1;
                    SomeTimer.Tag = new AnimateMsgBox(formSize, style);
                    break;

                case MsgBox.AnimateStyle.FadeIn:
                    MsgBoxx.Size = formSize;
                    MsgBoxx.Opacity = 0;
                    SomeTimer.Interval = 20;
                    SomeTimer.Tag = new AnimateMsgBox(formSize, style);
                    break;

                case MsgBox.AnimateStyle.ZoomIn:
                    MsgBoxx.Size = new Size(formSize.Width + 100, formSize.Height + 100);
                    SomeTimer.Tag = new AnimateMsgBox(formSize, style);
                    SomeTimer.Interval = 1;
                    break;
            }

            SomeTimer.Tick += timer_Tick;
            SomeTimer.Start();

            MsgBoxx.ShowDialog();
            MessageBeep(0);
            return ButtonResult;
        }

        static void timer_Tick(object sender, EventArgs e)
        {
            Timer timer = (Timer)sender;
            AnimateMsgBox animate = (AnimateMsgBox)timer.Tag;

            switch (animate.Style)
            {
                case MsgBox.AnimateStyle.SlideDown:
                    if (MsgBoxx.Height < animate.FormSize.Height)
                    {
                        MsgBoxx.Height += 17;
                        MsgBoxx.Invalidate();
                    }
                    else
                    {
                        SomeTimer.Stop();
                        SomeTimer.Dispose();
                    }
                    break;

                case MsgBox.AnimateStyle.FadeIn:
                    if (MsgBoxx.Opacity < 1)
                    {
                        MsgBoxx.Opacity += 0.1;
                        MsgBoxx.Invalidate();
                    }
                    else
                    {
                        SomeTimer.Stop();
                        SomeTimer.Dispose();
                    }
                    break;

                case MsgBox.AnimateStyle.ZoomIn:
                    if (MsgBoxx.Width > animate.FormSize.Width)
                    {
                        MsgBoxx.Width -= 17;
                        MsgBoxx.Invalidate();
                    }
                    if (MsgBoxx.Height > animate.FormSize.Height)
                    {
                        MsgBoxx.Height -= 17;
                        MsgBoxx.Invalidate();
                    }
                    break;
            }
        }

        private static void InitButtons(Buttons buttons)
        {
            switch (buttons)
            {
                case MsgBox.Buttons.AbortRetryIgnore:
                    MsgBoxx.InitAbortRetryIgnoreButtons();
                    break;

                case MsgBox.Buttons.OK:
                    MsgBoxx.InitOKButton();
                    break;

                case MsgBox.Buttons.OKCancel:
                    MsgBoxx.InitOKCancelButtons();
                    break;

                case MsgBox.Buttons.RetryCancel:
                    MsgBoxx.InitRetryCancelButtons();
                    break;

                case MsgBox.Buttons.YesNo:
                    MsgBoxx.InitYesNoButtons();
                    break;

                case MsgBox.Buttons.YesNoCancel:
                    MsgBoxx.InitYesNoCancelButtons();
                    break;
            }

            foreach (Button btn in MsgBoxx.ButtonCollection)
            {
                btn.ForeColor = Color.FromArgb(170, 170, 170);
                btn.Font = new System.Drawing.Font("Segoe UI", 8);
                btn.Padding = new Padding(3);
                btn.FlatStyle = FlatStyle.Flat;
                btn.Height = 30;
                btn.FlatAppearance.BorderColor = Color.FromArgb(99, 99, 98);

                MsgBoxx.FlowLayoutPanel.Controls.Add(btn);
            }
        }

        private static void InitIcon(Icon icon)
        {
            switch (icon)
            {
                case MsgBox.Icon.Application:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Application.ToBitmap();
                    break;

                case MsgBox.Icon.Exclamation:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Exclamation.ToBitmap();
                    break;

                case MsgBox.Icon.Error:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Error.ToBitmap();
                    break;

                case MsgBox.Icon.Info:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Information.ToBitmap();
                    break;

                case MsgBox.Icon.Question:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Question.ToBitmap();
                    break;

                case MsgBox.Icon.Shield:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Shield.ToBitmap();
                    break;

                case MsgBox.Icon.Warning:
                    MsgBoxx.PictureIcon.Image = SystemIcons.Warning.ToBitmap();
                    break;
            }
        }

        private void InitAbortRetryIgnoreButtons()
        {
            Button btnAbort = new Button();
            btnAbort.Text = "Abort";
            btnAbort.Click += ButtonClick;

            Button btnRetry = new Button();
            btnRetry.Text = "Retry";
            btnRetry.Click += ButtonClick;

            Button btnIgnore = new Button();
            btnIgnore.Text = "Ignore";
            btnIgnore.Click += ButtonClick;

            this.ButtonCollection.Add(btnAbort);
            this.ButtonCollection.Add(btnRetry);
            this.ButtonCollection.Add(btnIgnore);
        }

        private void InitOKButton()
        {
            Button btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Click += ButtonClick;

            this.ButtonCollection.Add(btnOK);
        }

        private void InitOKCancelButtons()
        {
            Button btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Click += ButtonClick;

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Click += ButtonClick;


            this.ButtonCollection.Add(btnOK);
            this.ButtonCollection.Add(btnCancel);
        }

        private void InitRetryCancelButtons()
        {
            Button btnRetry = new Button();
            btnRetry.Text = "OK";
            btnRetry.Click += ButtonClick;

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Click += ButtonClick;


            this.ButtonCollection.Add(btnRetry);
            this.ButtonCollection.Add(btnCancel);
        }

        private void InitYesNoButtons()
        {
            Button btnYes = new Button();
            btnYes.Text = "Yes";
            btnYes.Click += ButtonClick;

            Button btnNo = new Button();
            btnNo.Text = "No";
            btnNo.Click += ButtonClick;


            this.ButtonCollection.Add(btnYes);
            this.ButtonCollection.Add(btnNo);
        }

        private void InitYesNoCancelButtons()
        {
            Button btnYes = new Button();
            btnYes.Text = "Abort";
            btnYes.Click += ButtonClick;

            Button btnNo = new Button();
            btnNo.Text = "Retry";
            btnNo.Click += ButtonClick;

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Click += ButtonClick;

            this.ButtonCollection.Add(btnYes);
            this.ButtonCollection.Add(btnNo);
            this.ButtonCollection.Add(btnCancel);
        }

        private static void ButtonClick(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            switch (btn.Text)
            {
                case "Abort":
                    ButtonResult = DialogResult.Abort;
                    break;

                case "Retry":
                    ButtonResult = DialogResult.Retry;
                    break;

                case "Ignore":
                    ButtonResult = DialogResult.Ignore;
                    break;

                case "OK":
                    ButtonResult = DialogResult.OK;
                    break;

                case "Cancel":
                    ButtonResult = DialogResult.Cancel;
                    break;

                case "Yes":
                    ButtonResult = DialogResult.Yes;
                    break;

                case "No":
                    ButtonResult = DialogResult.No;
                    break;
            }

            MsgBoxx.Dispose();
        }

        private static Size MessageSize(string message)
        {
            Graphics g = MsgBoxx.CreateGraphics();
            int width = 350;
            int height = 230;

            SizeF size = g.MeasureString(message, new System.Drawing.Font("Segoe UI", 10));

            if (message.Length < 150)
            {
                if ((int)size.Width > 350)
                {
                    width = (int)size.Width;
                }
            }
            else
            {
                string[] groups = (from Match m in Regex.Matches(message, ".{1,180}") select m.Value).ToArray();
                int lines = groups.Length + 1;
                width = 700;
                height += (int)(size.Height + 10) * lines;
            }
            return new Size(width, height);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            Rectangle rect = new Rectangle(new Point(0, 0), new Size(this.Width - 1, this.Height - 1));
            Pen pen = new Pen(Color.FromArgb(0, 151, 251));

            g.DrawRectangle(pen, rect);
        }

        public enum Buttons
        {
            AbortRetryIgnore = 1,
            OK = 2,
            OKCancel = 3,
            RetryCancel = 4,
            YesNo = 5,
            YesNoCancel = 6
        }

        public enum Icon
        {
            Application = 1,
            Exclamation = 2,
            Error = 3,
            Warning = 4,
            Info = 5,
            Question = 6,
            Shield = 7,
            Search = 8
        }

        public enum AnimateStyle
        {
            SlideDown = 1,
            FadeIn = 2,
            ZoomIn = 3
        }

    }

    class AnimateMsgBox
    {
        public Size FormSize;
        public MsgBox.AnimateStyle Style;

        public AnimateMsgBox(Size formSize, MsgBox.AnimateStyle style)
        {
            this.FormSize = formSize;
            this.Style = style;
        }
    }
}