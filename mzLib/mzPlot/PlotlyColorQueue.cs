using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace mzPlot;

public sealed class PlotlyColorQueue : ColorQueue<Plotly.NET.Color>
{
    public PlotlyColorQueue(int setCapacity = 5) : base(setCapacity) { }

    protected override Plotly.NET.Color ConvertFromColor(Color color) => Plotly.NET.Color.fromRGB(color.R, color.G, color.B);

    protected override List<Plotly.NET.Color> ConvertListFromColor(List<Color> colorSet) => colorSet.Select(ConvertFromColor).ToList();
}

public abstract class ColorQueue<T>
{
    private readonly int _setQueueCapacity;
    protected Queue<T> Colors { get; }
    protected Queue<List<T>> ColorSets { get; }
    protected abstract T ConvertFromColor(Color color);
    protected abstract List<T> ConvertListFromColor(List<Color> colorSet);

    #region Default Colors

    protected static Color[] ColorArray = new[]
    {
        Color.Blue,
        Color.Green,
        Color.Purple,
        Color.Orange,
        Color.Yellow,
        Color.Cyan,
        Color.Magenta,
        Color.Lime,
        Color.Pink,
        Color.Teal,
        Color.Lavender,
        Color.Brown,
        Color.Beige,
        Color.Maroon,
        Color.Olive,
        Color.Coral,
        Color.Navy,
        Color.Gray,
        Color.White,
        Color.Black,
        Color.Indigo,
        Color.Turquoise,
        Color.DarkOrange,
        Color.DarkBlue,
        Color.DarkRed,
        Color.DarkGreen,
        Color.DarkViolet,
        Color.DarkCyan,
        Color.DarkMagenta,
        Color.DarkGray,
        Color.AliceBlue,
        Color.AntiqueWhite,
        Color.Aqua,
        Color.Aquamarine,
        Color.Azure,
        Color.Bisque,
        Color.BlanchedAlmond,
        Color.BurlyWood,
        Color.CadetBlue,
        Color.Chartreuse,
        Color.Chocolate,
        Color.CornflowerBlue,
        Color.Crimson,
        Color.DarkGoldenrod,
        Color.DarkKhaki,
        Color.DarkSalmon,
        Color.DarkSeaGreen,
        Color.DarkSlateBlue,
        Color.DarkSlateGray,
        Color.DarkTurquoise,
        Color.DeepPink,
        Color.DeepSkyBlue,
        Color.DimGray,
        Color.DodgerBlue,
        Color.Firebrick,
        Color.FloralWhite,
        Color.ForestGreen,
        Color.Fuchsia,
        Color.Gainsboro,
        Color.GhostWhite,
        Color.Gold,
        Color.Goldenrod,
        Color.GreenYellow,
        Color.IndianRed,
        Color.Ivory,
        Color.Khaki,
        Color.LavenderBlush,
        Color.LawnGreen,
        Color.LemonChiffon,
        Color.LightBlue,
        Color.LightCoral,
        Color.LightCyan,
        Color.LightGoldenrodYellow,
        Color.LightGray,
        Color.LightGreen,
        Color.LightPink,
        Color.LightSteelBlue,
        Color.LightYellow,
        Color.Linen,
        Color.MediumBlue,
        Color.MediumOrchid,
        Color.MediumPurple,
        Color.MediumSeaGreen,
        Color.MediumSlateBlue,
        Color.MediumSpringGreen,
        Color.MediumTurquoise,
        Color.MediumVioletRed,
        Color.MidnightBlue,
        Color.MintCream,
        Color.MistyRose,
        Color.Moccasin,
        Color.NavajoWhite,
        Color.OldLace,
        Color.OliveDrab,
        Color.OrangeRed,
        Color.Orchid,
        Color.PaleGoldenrod,
        Color.PaleGreen,
        Color.PaleTurquoise,
        Color.PaleVioletRed,
        Color.PapayaWhip,
        Color.PeachPuff,
        Color.Peru,
        Color.Plum,
        Color.PowderBlue,
        Color.RosyBrown,
        Color.RoyalBlue,
        Color.SaddleBrown,
        Color.Salmon,
        Color.SandyBrown,
        Color.SeaGreen,
        Color.SeaShell,
        Color.Sienna,
        Color.Silver,
        Color.SlateBlue,
        Color.SlateGray,
        Color.Snow,
        Color.SpringGreen,
        Color.SteelBlue,
        Color.Tan,
        Color.Thistle,
        Color.Tomato,
        Color.Violet,
        Color.Wheat,
        Color.WhiteSmoke,
        Color.YellowGreen
    };

    protected static Color[] SetBaseArray = new[]
    {
        Color.RoyalBlue,
        Color.IndianRed,
        Color.MediumSpringGreen,
        Color.Orchid,
        Color.Orange,
        Color.Cyan,
        Color.Yellow,
        Color.Magenta,
        Color.Lime,
        Color.Pink,
        Color.Teal
    };

    #endregion

    protected ColorQueue(int setCapacity = 5)
    {
        _setQueueCapacity = setCapacity;
        Colors = new();
        ColorSets = new();
        BuildQueues();
    }

    public T Dequeue()
    {
        var color = Colors.Dequeue();
        Colors.Enqueue(color);
        return color;
    }

    public List<T> DequeueSet()
    {
        var colorSet = ColorSets.Dequeue();
        ColorSets.Enqueue(colorSet);
        return colorSet;
    }

    protected List<Color> GenerateColorSet(Color baseColor)
    {
        var colorSet = new List<Color> { baseColor };
        for (int i = 1; i <= _setQueueCapacity; i++)
        {
            colorSet.Add(DarkenColor(baseColor, i * 0.2));
        }
        return colorSet;
    }

    protected Color DarkenColor(Color color, double factor)
    {
        var r = (int)(color.R * (1 - factor));
        var g = (int)(color.G * (1 - factor));
        var b = (int)(color.B * (1 - factor));
        return Color.FromArgb(color.A, Math.Max(r, 0), Math.Max(g, 0), Math.Max(b, 0));
    }

    private void BuildQueues()
    {
        foreach (var color in ColorArray)
        {
            Colors.Enqueue(ConvertFromColor(color));
        }

        foreach (var baseColor in SetBaseArray)
        {
            ColorSets.Enqueue(ConvertListFromColor(GenerateColorSet(baseColor)));
        }
    }
}