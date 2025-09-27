using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Services;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace FlatRedBall.AnimationEditorForms.Controls;

public partial class WireframeEditControl : UserControl
{
    #region Fields

    List<int> mAvailableZoomLevels = new List<int>();

    #endregion

    #region Events

    public event EventHandler ZoomChanged;

    #endregion

    #region Properties

    public List<int> AvailableZoomLevels
    {
        get
        {
            return mAvailableZoomLevels;
        }
    }

    public int PercentageValue
    {
        get
        {
            if(ComboBox.SelectedIndex >= 0 && ComboBox.SelectedIndex < mAvailableZoomLevels.Count)
            {
                return mAvailableZoomLevels[ComboBox.SelectedIndex];
            }
            else
            {
                return 100;
            }
        }
        set
        {
            var index = mAvailableZoomLevels.IndexOf(value);

            if(index == -1)
            {
                index = mAvailableZoomLevels.IndexOf(100);
            }

            ComboBox.SelectedIndex = value;
        }
    }

    int CurrentZoomIndex
    {
        get
        {
            return mAvailableZoomLevels.IndexOf(PercentageValue);
        }
    }

    #endregion

    public WireframeEditControl()
    {
        InitializeComponent();
        InitializeComboBox();
    }



    private void InitializeComboBox()
    {
        mAvailableZoomLevels.Add(1600);
        mAvailableZoomLevels.Add(1200);
        mAvailableZoomLevels.Add(1000);
        mAvailableZoomLevels.Add(800);
        mAvailableZoomLevels.Add(700);
        mAvailableZoomLevels.Add(600);
        mAvailableZoomLevels.Add(500);
        mAvailableZoomLevels.Add(400);
        mAvailableZoomLevels.Add(350);
        mAvailableZoomLevels.Add(300);
        mAvailableZoomLevels.Add(250);
        mAvailableZoomLevels.Add(200);
        mAvailableZoomLevels.Add(175);
        mAvailableZoomLevels.Add(150);
        mAvailableZoomLevels.Add(125);
        mAvailableZoomLevels.Add(100);
        mAvailableZoomLevels.Add(87);
        mAvailableZoomLevels.Add(75);
        mAvailableZoomLevels.Add(63);
        mAvailableZoomLevels.Add(50);
        mAvailableZoomLevels.Add(33);
        mAvailableZoomLevels.Add(25);
        mAvailableZoomLevels.Add(10);
        mAvailableZoomLevels.Add(5);

        foreach (var value in mAvailableZoomLevels)
        {
            ComboBox.Items.Add(value.ToString() + "%");
        }

        ComboBox.Text = "100%";
    }

    private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (this.ZoomChanged != null)
        {
            ZoomChanged(this, null);
        }
    }

    public void ZoomOut()
    {
        int index = CurrentZoomIndex;

        if (index < mAvailableZoomLevels.Count - 1)
        {
            index++;
            ComboBox.SelectedIndex = index;
        }
    }

    public void ZoomIn()
    {
        int index = CurrentZoomIndex;

        if (index > 0)
        {
            index--;
            ComboBox.SelectedIndex = index;
        }
    }
}
