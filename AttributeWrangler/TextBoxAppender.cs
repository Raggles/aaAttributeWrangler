using log4net.Appender;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AttributeWrangler
{
    public class TextBoxAppender : AppenderSkeleton
    {
        private TextBox _textBox;
        public TextBox AppenderTextBox
        {
            get
            {
                return _textBox;
            }
            set
            {
                _textBox = value;
            }
        }
        public string FormName { get; set; }
        public string TextBoxName { get; set; }

        private Control FindControlRecursive(Window root, string textBoxName)
        {
            foreach (UIElement item in ((Grid)root.Content).Children)
            {
                Control c = item as Control;
                if (c != null)
                {
                    if (c.Name == textBoxName)
                        return c;
                }
            }
            return null;
        }

        protected override void Append(log4net.Core.LoggingEvent loggingEvent)
        {
            try
            {
                if (_textBox == null)
                {
                    if (String.IsNullOrEmpty(FormName) ||
                        String.IsNullOrEmpty(TextBoxName))
                    {
                        return;
                    }

                    Window w = Application.Current.Windows.OfType<Window>().Where(x => x.Title == FormName).FirstOrDefault();
                    if (w == null)
                    {
                        return;
                    }
                    _textBox = (TextBox)FindControlRecursive(w, TextBoxName);
                    if (_textBox == null)
                    {
                        return;
                    }
                    w.Closing += (s, e) => _textBox = null;
                }

                _textBox.Dispatcher.BeginInvoke((Action)delegate
                {
                    _textBox.AppendText(RenderLoggingEvent(loggingEvent));
                    _textBox.ScrollToEnd();
                });
            }
            catch {}
        }
    }

}