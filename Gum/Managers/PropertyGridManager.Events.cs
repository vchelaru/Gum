using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Managers
{


    public partial class PropertyGridManager
    {
        DataUiGrid mEventsDataGrid;

        public void InitializeEvents(DataUiGrid eventsDataUiGrid)
        {
            mEventsDataGrid = eventsDataUiGrid;

            
        }


        private void RefreshEventsUi()
        {
            if (SelectedState.Self.SelectedInstances.GetCount() > 1)
            {
                mEventsDataGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                //List<MemberCategory> categories = GetEventCategories();

                EventsViewModel viewModel = new EventsViewModel();

                var selectedInstance = SelectedState.Self.SelectedInstance;
                var selectedElement = SelectedState.Self.SelectedElement;
                viewModel.InstanceSave = selectedInstance;
                viewModel.ElementSave = selectedElement ;

                mEventsDataGrid.Instance = viewModel;
                mEventsDataGrid.MembersToIgnore.Add("InstanceSave");
                mEventsDataGrid.MembersToIgnore.Add("ElementSave");
                mEventsDataGrid.Categories[0].Name = "Events on this";
                if (SelectedState.Self.SelectedInstance != null)
                {
                    // Now loop through all objects and give them an Expose right click option
                    foreach (var category in mEventsDataGrid.Categories)
                    {
                        foreach (var member in category.Members)
                        {

                            member.ContextMenuEvents.Add("Expose Event", HandleExposeClick);
                        }
                    }


                    var instanceElement = ObjectFinder.Self.GetElementSave(SelectedState.Self.SelectedInstance.BaseType);

                    if (instanceElement != null)
                    {
                        foreach (var eventSaveInBase in instanceElement.Events.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                        {
                            var eventSave = SelectedState.Self.SelectedElement.Events.FirstOrDefault(item => item.Name ==
                                selectedInstance.Name + "." + eventSaveInBase.ExposedAsName);

                            if (eventSave == null)
                            {
                                eventSave = new EventSave();
                                eventSave.Name = selectedInstance.Name + "." + eventSaveInBase.ExposedAsName;
                                selectedElement.Events.Add(eventSave);
                            }



                            EventInstanceMember instanceMember = new EventInstanceMember(
                                SelectedState.Self.SelectedElement,
                                SelectedState.Self.SelectedInstance,
                                eventSave);

                            mEventsDataGrid.Categories[0].Members.Add(instanceMember);
                        }
                    }

                }
                else
                {
                    MemberCategory exposed = new MemberCategory();
                    exposed.Name = "Exposed";

                    foreach (var eventSave in SelectedState.Self.SelectedElement.Events.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                    {
                        EventInstanceMember instanceMember = new EventInstanceMember(
                            SelectedState.Self.SelectedElement,
                            SelectedState.Self.SelectedInstance,
                            eventSave);

                        exposed.Members.Add(instanceMember);

                    }

                    mEventsDataGrid.Categories.Add(exposed);

                }

                mEventsDataGrid.Visibility = System.Windows.Visibility.Visible;

                mEventsDataGrid.Refresh();


            }
        }

        private void HandleExposeClick(object sender, System.Windows.RoutedEventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();

            InstanceMember instanceMember = ((System.Windows.Controls.MenuItem)sender).Tag as InstanceMember;

            tiw.Message = "Enter name to expose as:";

            tiw.Result = SelectedState.Self.SelectedInstance.Name + instanceMember.Name;

            var dialogResult = tiw.ShowDialog();


            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                string nameWithDot = SelectedState.Self.SelectedInstance.Name + "." + instanceMember.Name;

                EventSave eventSave = SelectedState.Self.SelectedElement.Events.FirstOrDefault(item => item.Name == nameWithDot);

                if (eventSave == null)
                {
                    eventSave = new EventSave();
                    eventSave.Name = nameWithDot;

                    SelectedState.Self.SelectedElement.Events.Add(eventSave);
                }
                eventSave.ExposedAsName = tiw.Result;

                if (SelectedState.Self.SelectedElement != null)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }
            }
        }


    }
}
