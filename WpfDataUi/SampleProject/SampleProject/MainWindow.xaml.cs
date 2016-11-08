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
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace SampleProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            InitializeDataGrid();
        }

        private void InitializeDataGrid()
        {
            Character character = new Character();

            character.MaxHealth = 100;
            character.CurrentHealth = 33;

            this.DataGrid.Instance = character;

            // At this point the UI has been populated, but we can make modifications. 

            // Like we can add a new category with some unused properties:
            var category = new MemberCategory("Custom Category");

            CreateAlways3Member(character, category);

            CreateTwiceXMember(character, category);

            CreateYAsAngle(character, category);

            SetHealthDisplayToUseSlider(character, DataGrid);

            this.DataGrid.Categories.Add(category);
        }

        private void SetHealthDisplayToUseSlider(Character character, DataUiGrid dataGrid)
        {
            // remove the max health member, we only want to expose current health:
            var category = dataGrid.Categories.First();
            var maxHealthMember = category.Members.First(item => item.Name == nameof(character.MaxHealth));
            category.Members.Remove(maxHealthMember);

            // Find the current health property:
            var currentHealthMember = category.Members.First(item => item.Name == nameof(character.CurrentHealth));
            // Set its preferred UI displayer as the SliderDisplay
            currentHealthMember.PreferredDisplayer = typeof(SliderDisplay);
            // Set the SliderDisplay to the character's MaxHealth
            currentHealthMember.UiCreated += (control) => ((SliderDisplay)control).MaxValue = character.MaxHealth;
        }

        private static void CreateAlways3Member(Character character, MemberCategory category)
        {
            // This shows how to create a custom member. It doesn't do much since it has no
            // backing data - it is of type int, ignores any input, and will always display 3
            var member = new InstanceMember("I'm always 3", character);
            // this property always returns 3
            member.CustomGetEvent += (characterInstance) => 3;
            member.CustomSetEvent += (characterInstance, value) => { /*Could assign a value here*/ };
            member.CustomGetTypeEvent += (characterInstance) => typeof(int); // This determines the (default) control to create
            category.Members.Add(member);
        }

        private void CreateTwiceXMember(Character character, MemberCategory category)
        {
            // This shows how to create a custom member. It doesn't do much since it has no
            // backing data - it is of type int, ignores any input, and will always display 3
            var member = new InstanceMember("X * 2", character);
            // this property always returns 3
            member.CustomGetEvent += (characterInstance) => character.X*2;
            member.CustomSetEvent += (characterInstance, value) =>
            {
                character.X = (float)value / 2.0f;
                // The grid doesn't automatically refresh, so we have to tell it to, so X will get updated:
                this.DataGrid.Refresh();
            };
            member.CustomGetTypeEvent += (characterInstance) => typeof(float); // This determines the (default) control to create
            category.Members.Add(member);

        }

        private void CreateYAsAngle(Character character, MemberCategory category)
        {
            var member = new InstanceMember("Y as Angle", character);
            // this property always returns 3
            member.CustomGetEvent += (characterInstance) => character.Y;
            member.CustomSetEvent += (characterInstance, value) =>
            {
                character.Y = (float)value;
                // The grid doesn't automatically refresh, so we have to tell it to, so X will get updated:
                this.DataGrid.Refresh();
            };
            member.CustomGetTypeEvent += (characterInstance) => typeof(float); // This determines the (default) control to create

            member.PreferredDisplayer = typeof(AngleSelectorDisplay);

            // UiCreated lets us perform custom logic on the control right after it's created. In this case
            // we're telling the angle control that we want degrees pushed to our backing data (rather than
            // converting to radians).
            member.UiCreated += (control) => ((AngleSelectorDisplay)control).TypeToPushToInstance = AngleType.Degrees;

            category.Members.Add(member);
        }


    }
}
