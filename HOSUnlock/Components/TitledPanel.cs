using System;
using Terminal.Gui;

namespace HOSUnlock.Components;

/// <summary>
/// A reusable panel component with a title and content area
/// </summary>
public class TitledPanel : FrameView
{
    private View contentView;

    public TitledPanel(string title)
    {
        this.Title = title;
        this.Border.BorderStyle = BorderStyle.Single;
        this.Border.Effect3D = false;
        
        // Create a content view that fills the panel
        contentView = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(0),
            Height = Dim.Fill(0)
        };
        
        Add(contentView);
    }

    /// <summary>
    /// Gets the content view where you can add your controls
    /// </summary>
    public View Content => contentView;

    /// <summary>
    /// Helper method to add content to the panel
    /// </summary>
    public void AddContent(View view)
    {
        contentView.Add(view);
    }

    /// <summary>
    /// Helper method to clear all content from the panel
    /// </summary>
    public void ClearContent()
    {
        contentView.RemoveAll();
    }
}
