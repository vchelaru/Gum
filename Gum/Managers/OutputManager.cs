using System;
using System.Windows.Controls;

namespace Gum.Managers
{
    public class OutputManager : Singleton<OutputManager>
    {
        TextBox mTextBox;

        const int MaxCharacterLength = 50000;

        public void Initialize(TextBox textBox)
        {
            if(textBox == null)
            {
                throw new ArgumentNullException(nameof(textBox));
            }
            mTextBox = textBox;
        }


        public void AddOutput(string whatToAdd)
        {
            if (mTextBox == null) return;

            string text = mTextBox.Text;
            text += "\n[" + DateTime.Now.ToShortTimeString() + "] " + whatToAdd;

            if (text.Length > MaxCharacterLength)
            {
                text = text.Substring(MaxCharacterLength / 2);
            }

            mTextBox.Text = text;
            ScrollToBottom();
        }

        public void AddError(string whatToAdd)
        {
            string text = mTextBox.Text;
            text += "\n[" + DateTime.Now.ToShortTimeString() + "] ERROR:  " + whatToAdd;

            if (text.Length > MaxCharacterLength)
            {
                text = text.Substring(MaxCharacterLength / 2);
            }

            mTextBox.Text = text;
            ScrollToBottom();
        }


        private void ScrollToBottom()
        {
            mTextBox.Select(mTextBox.Text.Length, 0);

            try
            {
                mTextBox.ScrollToEnd();
            }
            catch
            {
                // do nothing
            }
        }
    }
}
