using Meadow;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.MicroLayout;
using Meadow.Peripherals.Displays;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MeadowDesktopAIApp;

public class DisplayController
{
    private readonly DisplayScreen displayScreen;
    private int CurrentImagePositionLeft = 0;
    private int CurrentImagePositionTop = 0;

    public DisplayController(IPixelDisplay display)
    {
        displayScreen = new DisplayScreen(display)
        {
            BackgroundColor = Color.FromHex("14607F")
        };

        displayScreen.Controls.Add(new Label(
            left: 0,
            top: 0,
            width: displayScreen.Width,
            height: displayScreen.Height)
        {
            Text = "Hello World",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Font = new Font12x20()
        });
    }

    public void DrawImage(Image image)
    {
        displayScreen.Controls.Clear();

        displayScreen.Controls.Add(new Picture(
            left: 0,
            top: 0,
            width: displayScreen.Width,
            height: displayScreen.Height, image));
        CurrentImagePositionLeft = image.Width < displayScreen.Width ? (displayScreen.Width - image.Width)/2 : 0;
        CurrentImagePositionTop = image.Height < displayScreen.Height ? (displayScreen.Height - image.Height)/2 : 0;
    }

    public void DrawBox(int x, int y, int width, int height, Color color)
    {
        displayScreen.Controls.Add(new Box(x, y, width, height){ ForeColor = color, IsFilled = false});
    }

    public void DrawMLBox(int x, int y, int width, int height, string label, float score, Color color)
    {        
        displayScreen.Controls.Add(new Box(x+CurrentImagePositionLeft, y+CurrentImagePositionTop, width, height){ ForeColor = color, IsFilled = false});
        displayScreen.Controls.Add(new Box(x+CurrentImagePositionLeft+1, y+CurrentImagePositionTop+1, width-2, height-2){ ForeColor = color, IsFilled = false});
        displayScreen.Controls.Add(new Label(x+CurrentImagePositionLeft, y + height + 8+CurrentImagePositionTop, width, 8){ Text = $"{label} ({score*100:0}%)", Font = new Font12x16(), TextColor = color, HorizontalAlignment = HorizontalAlignment.Left});
    }
}