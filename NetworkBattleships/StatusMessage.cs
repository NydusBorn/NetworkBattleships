using System;
using Microsoft.UI.Xaml;

namespace NetworkBattleships;

public class StatusMessage
{
    public string Text { get; set; }
    public string DateTime { get; set; }
    public HorizontalAlignment Alignment { get; set; }

    public enum SideAlignment
    {
        Left,
        Right,
        Stretch
    }

    public StatusMessage(string message, SideAlignment alignment)
    {
        Text = message;
        Alignment = alignment switch
        {
            SideAlignment.Left => HorizontalAlignment.Left,
            SideAlignment.Right => HorizontalAlignment.Right,
            SideAlignment.Stretch => HorizontalAlignment.Stretch,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
        };
        DateTime = System.DateTime.Now.ToString("T");
    }
}