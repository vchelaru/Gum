using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gum.Managers
{
    public class OutputManager : Singleton<OutputManager>
    {
        RichTextBox mRichTextBox;

        const int MaxCharacterLength = 50000;

        public void Initialize(RichTextBox richTextBox)
        {
            if(richTextBox == null)
            {
                throw new ArgumentNullException(nameof(richTextBox));
            }
            mRichTextBox = richTextBox;
        }


        public void AddOutput(string whatToAdd)
        {
            string text = mRichTextBox.Text;
            text += "\n[" + DateTime.Now.ToShortTimeString() + "] " + whatToAdd;

            if (text.Length > MaxCharacterLength)
            {
                text = text.Substring(MaxCharacterLength / 2);
            }

            mRichTextBox.Text = text;
            ScrollToBottom();
        }

        public void AddError(string whatToAdd)
        {
            string text = mRichTextBox.Text;
            text += "\n[" + DateTime.Now.ToShortTimeString() + "] ERROR:  " + whatToAdd;

            if (text.Length > MaxCharacterLength)
            {
                text = text.Substring(MaxCharacterLength / 2);
            }

            mRichTextBox.Text = text;
            ScrollToBottom();
        }


        private void ScrollToBottom()
        {
            mRichTextBox.Select(mRichTextBox.TextLength, 0);

            try
            {
                mRichTextBox.ScrollToCaret();
            }
            catch
            {
                // do nothing
            }
        }
    }
}
