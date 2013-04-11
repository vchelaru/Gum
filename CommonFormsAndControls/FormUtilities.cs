using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace CommonFormsAndControls
{
    public class FormUtilities
    {
        static FormUtilities mSelf;
        public static FormUtilities Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new FormUtilities();
                }
                return mSelf;
            }
        }


        public void PositionTopLeftToCursor<T>(T instance) where T : Form
        {
            
            instance.StartPosition = FormStartPosition.Manual;
            instance.Location = new Point(Form.MousePosition.X, Form.MousePosition.Y);

        }

        public void PositionCenterToCursor<T>(T instance) where T : Form
        {

            instance.StartPosition = FormStartPosition.Manual;
            instance.Location = new Point(Form.MousePosition.X - instance.Width/2, 
                Form.MousePosition.Y - instance.Height/2);

        }
    }
}
