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

            Form myForm;
            Screen myScreen = Screen.FromControl(instance);
            Rectangle area = myScreen.WorkingArea;

            const int borderBuffer = 80;
            var screenBottom = area.Height - borderBuffer; // On Vic's device this is inaccurate, it's a little too big. Leave room for bar and give it a buffer.

            var desiredX = 
                System.Math.Max(0, Form.MousePosition.X - instance.Width / 2);

            var desiredY = Form.MousePosition.Y - instance.Height / 2;
            var bottomOverlap = (desiredY + instance.Height / 2) - screenBottom;

            if(bottomOverlap > 0)
            {
                desiredY -= bottomOverlap;
            }

            instance.Location = new Point(desiredX, desiredY);

        }
    }
}
