using System;
using Meadow;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Foundation.Sensors.Hid;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Buttons;

namespace MeadowDesktopAIApp;

public class InputController
{
    private readonly Keyboard keyboard;
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? EnterRequested;

    public IButton? RightButton { get; }
    public IButton? LeftButton { get; }
    public IButton? EnterButton { get; }

    public InputController()
    {
        keyboard = new Keyboard();
        LeftButton = new PushButton(keyboard.Pins.Left.CreateDigitalInterruptPort(InterruptMode.EdgeFalling));
        RightButton = new PushButton(keyboard.Pins.Right.CreateDigitalInterruptPort(InterruptMode.EdgeFalling));
        EnterButton = new PushButton(keyboard.Pins.Enter.CreateDigitalInterruptPort(InterruptMode.EdgeFalling));

        if (LeftButton is { } ub)
        {
            ub.PressStarted += (s, e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        }
        if (RightButton is { } db)
        {
            db.PressStarted += (s, e) => NextRequested?.Invoke(this, EventArgs.Empty);
        }
        if (EnterButton is { } eb)
        {
            eb.PressStarted += (s, e) => EnterRequested?.Invoke(this, EventArgs.Empty);
        }   
    }
}
