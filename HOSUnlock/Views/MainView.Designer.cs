using System;
using HOSUnlock.Components;
using HOSUnlock.Constants;
using Terminal.Gui;

namespace HOSUnlock.Views;

public partial class MainView : Terminal.Gui.Window, IDisposable
{
    private TitledPanel topLeftPanel;
    private TitledPanel topRightPanel;
    private TitledPanel bottomLeftPanel;
    private TitledPanel bottomRightPanel;

    private TitledPanel bottomOptionsPanel;

    private void InitializeComponent()
    {
        this.Width = Dim.Fill(0);
        this.Height = Dim.Fill(0);
        this.X = 0;
        this.Y = 0;
        this.Modal = false;
        this.Text = "";
        this.Border.BorderStyle = Terminal.Gui.BorderStyle.Single;
        this.Border.Effect3D = false;
        this.Border.DrawMarginFrame = true;
        this.TextAlignment = Terminal.Gui.TextAlignment.Left;
        this.Title = "HOSUnlock - Ctrl+Q to quit";

        // Create four equal panels in a 2x2 grid
        topLeftPanel = new TitledPanel("Panel 1")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Percent(50) - 3,
        };

        topRightPanel = new TitledPanel("Panel 2")
        {
            X = Pos.Right(topLeftPanel),
            Y = 0,
            Width = Dim.Fill(0),
            Height = Dim.Percent(50) - 3
        };

        bottomLeftPanel = new TitledPanel("Panel 3")
        {
            X = 0,
            Y = Pos.Bottom(topLeftPanel),
            Width = Dim.Percent(50),
            Height = Dim.Fill(0) - 3
        };

        bottomRightPanel = new TitledPanel("Panel 4")
        {
            X = Pos.Right(bottomLeftPanel),
            Y = Pos.Bottom(topRightPanel),
            Width = Dim.Fill(0),
            Height = Dim.Fill(0) - 3
        };

        bottomOptionsPanel = new TitledPanel("Options")
        {
            X = 0,
            Y = Pos.Bottom(bottomLeftPanel),
            Width = Dim.Fill(0),
            Height = 3
        };

        // Add panels to the main view
        Add(topLeftPanel, topRightPanel, bottomLeftPanel, bottomRightPanel, bottomOptionsPanel);
    }
}
