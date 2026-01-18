using System;
using HOSUnlock.Components;
using HOSUnlock.Constants;
using Terminal.Gui;

namespace HOSUnlock.Views;

public partial class MainView : Terminal.Gui.Window, IDisposable
{
    // Log panels for different purposes
    private LogPanel _statusPanel = null!;
    private LogPanel _thresholdsPanel = null!;
    private LogPanel _applicationLogsPanel = null!;
    private LogPanel _requestResultsPanel = null!;

    // Options panel (bottom bar)
    private FrameView _optionsPanel = null!;

    private void InitializeComponent()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        X = 0;
        Y = 0;
        Modal = false;
        Border.BorderStyle = BorderStyle.Single;
        Border.Effect3D = false;
        Border.DrawMarginFrame = true;
        TextAlignment = TextAlignment.Left;
        Title = "HOSUnlock - Ctrl+Q to quit";

        // Top-Left: Status & Configuration
        _statusPanel = new LogPanel("Status & Configuration")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Percent(50) - 1
        };

        // Top-Right: Threshold Times
        _thresholdsPanel = new LogPanel("Threshold Times")
        {
            X = Pos.Right(_statusPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50) - 1
        };

        // Bottom-Left: Application Logs
        _applicationLogsPanel = new LogPanel("Application Logs")
        {
            X = 0,
            Y = Pos.Bottom(_statusPanel),
            Width = Dim.Percent(50),
            Height = Dim.Fill() - 3
        };

        // Bottom-Right: Request Results
        _requestResultsPanel = new LogPanel("Request Results")
        {
            X = Pos.Right(_applicationLogsPanel),
            Y = Pos.Bottom(_thresholdsPanel),
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3
        };

        // Bottom: Options Bar
        _optionsPanel = new FrameView("Options")
        {
            X = 0,
            Y = Pos.Bottom(_applicationLogsPanel),
            Width = Dim.Fill(),
            Height = 3,
            Border = { BorderStyle = BorderStyle.Single }
        };

        Add(_statusPanel, _thresholdsPanel, _applicationLogsPanel, _requestResultsPanel, _optionsPanel);
    }
}
