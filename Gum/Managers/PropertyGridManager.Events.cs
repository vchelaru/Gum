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

            var selectedInstance = SelectedState.Self.SelectedInstance;
            var selectedElement = SelectedState.Self.SelectedElement;

            if (selectedElement == null)
            {
                mEventsDataGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                //List<MemberCategory> categories = GetEventCategories();
                mEventsDataGrid.Categories.Clear();

                var eventsOnThisCategory = new MemberCategory();
                eventsOnThisCategory.Name = "Events on this";
                mEventsDataGrid.Categories.Add(eventsOnThisCategory);

                if (SelectedState.Self.SelectedInstance != null)
                {
                    var instanceElement = ObjectFinder.Self.GetElementSave(SelectedState.Self.SelectedInstance.BaseType);

                    if (instanceElement != null)
                    {
                        foreach (var eventSaveInBase in instanceElement.Events.Where(item=>
                            item.Enabled
                            && ( string.IsNullOrEmpty(item.GetSourceObject()) || !string.IsNullOrEmpty(item.ExposedAsName))
                            ))
                        {
                            var eventSave = selectedElement.Events.FirstOrDefault(item => item.Name ==
                                selectedInstance.Name + "." + eventSaveInBase.GetExposedOrRootName());

                            if (eventSave == null)
                            {
                                eventSave = new EventSave();
                                eventSave.Name = selectedInstance.Name + "." + eventSaveInBase.GetExposedOrRootName();
                                // I don't think we want to add this here yet do we?
                                // We should add it only if the user checks it
                                //selectedElement.Events.Add(eventSave);
                            }



                            EventInstanceMember instanceMember = new EventInstanceMember(
                                selectedElement,
                                selectedInstance,
                                eventSave);

                            mEventsDataGrid.Categories[0].Members.Add(instanceMember);
                        }
                    }

                    // Now loop through all objects and give them an Expose right click option
                    foreach (var category in mEventsDataGrid.Categories)
                    {
                        foreach (var member in category.Members)
                        {
                            member.ContextMenuEvents.Add("Expose Event", HandleExposeClick);
                        }
                    }
                }
                else if(selectedElement != null)
                {
                    EventsViewModel viewModel = new EventsViewModel();

                    viewModel.InstanceSave = selectedInstance;
                    viewModel.ElementSave = selectedElement;

                    mEventsDataGrid.Instance = viewModel;
                    mEventsDataGrid.MembersToIgnore.Add("InstanceSave");
                    mEventsDataGrid.MembersToIgnore.Add("ElementSave");
                    mEventsDataGrid.Categories[0].Name = "Events on this";

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

            tiw.Result = SelectedState.Self.SelectedInstance.Name + instanceMember.DisplayName;

            var dialogResult = tiw.ShowDialog();


            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                string nameWithDot = SelectedState.Self.SelectedInstance.Name + "." + instanceMember.DisplayName;

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
