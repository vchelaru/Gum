using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using WpfDataUi.DataTypes;

namespace WpfDataUi
{
    /// <summary>
    /// Interaction logic for SingleDataUiContainer.xaml
    /// </summary>
    public partial class SingleDataUiContainer : UserControl
    {
        #region Fields
        KeyValuePair<Func<Type, bool>, Type> kvp;

        static List<KeyValuePair<Func<Type, bool>, Type>> mTypeDisplayerAssociation = new List<KeyValuePair<Func<Type, bool>, Type>>();

        #endregion

        #region Properties

        InstanceMember InstanceMember
        {
            get
            {
                return (InstanceMember)DataContext;
            }
        }

        object Instance
        {
            get
            {
                return InstanceMember.Instance;
            }
        }

        public static List<KeyValuePair<Func<Type, bool>, Type>> TypeDisplayerAssociation
        {
            get
            {
                return mTypeDisplayerAssociation;
            }
        }

        #endregion

        #region Constructor

        static SingleDataUiContainer()
        {
            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
             (item) => item == typeof(bool),
             typeof(CheckBoxDisplay))
             );


            mTypeDisplayerAssociation.Add(new KeyValuePair<Func<Type, bool>, Type>(
                (item) => item!= null && item.IsEnum,
                typeof(ComboBoxDisplay))
                );

        }
        public SingleDataUiContainer()
        {
            this.DataContextChanged += HandleDataContextChanged;
            InitializeComponent();
        }

        #endregion

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Grid.Children.Add(CreateInternalControl());
        }

        UserControl CreateInternalControl()
        {
            Type type = InstanceMember.PropertyType;



            UserControl controlToAdd = CreateControlByType(InstanceMember, type);

            return controlToAdd;
        }

        private UserControl CreateControlByType(InstanceMember instanceMember, Type type)
        {

            UserControl controlToAdd = null;

            if (instanceMember.PreferredDisplayer != null)
            {
                controlToAdd = (UserControl)Activator.CreateInstance(instanceMember.PreferredDisplayer);
            }

            if (controlToAdd == null)
            {
                foreach (var kvp in mTypeDisplayerAssociation)
                {
                    if (kvp.Key(type))
                    {
                        controlToAdd = (UserControl)Activator.CreateInstance(kvp.Value);
                    }
                }
            }


            if (controlToAdd == null)
            {
                controlToAdd = new TextBoxDisplay();
            }


            IDataUi display = (IDataUi)controlToAdd;
            display.InstanceMember = instanceMember;

            return controlToAdd;
        }

    }
}
