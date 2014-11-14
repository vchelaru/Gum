using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi.Controls;

namespace WpfDataUi
{

    public enum SomeEnumeration
    {
        First,
        Second,
        Third,
        Fourth,
    }



    public class TestClass
    {
        public string FirstName;
        public string LastName;

        public int Age;
        public int Age1;
        public int Age2;
        public int Age3;
        public int Age4;
        public int Age5;
        public int Age6;
        public int Age7;
        public int Age8;
        public int Age9;
        public int Agea;
        public int Ageb;
        public int Agec;
        public int Aged;
        public int Agee;
        public int Agef;

        public bool IsHungry;

        public SomeEnumeration Order;
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var instance = new TestClass();

            instance.FirstName = "Test name!!";
            instance.LastName = "LastNameYup";

            Grid.Instance = instance;

            //TextBoxDisplay tbd = new TextBoxDisplay();
            //tbd.PropertyName = "LastName";
            //tbd.Instance = instance;
            //Grid.AddControl(tbd);

            //tbd = new TextBoxDisplay();
            //tbd.PropertyName = "FirstName";
            //tbd.Instance = instance;
            //Grid.AddControl(tbd);
        }
    }
}
