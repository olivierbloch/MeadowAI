using System;
using System.Threading;
using System.Threading.Tasks;


namespace reTerminalApp;

public class InputController
{
    public event EventHandler NextRequested;
    public event EventHandler PreviousRequested;
    public event EventHandler EnterRequested;

    public void StartListeningForKeyboardEvents()
    {
        Task KeyboardListener = new Task(() => ListenForKeyboardEvents());
        KeyboardListener.Start();
    }

    public Task ListenForKeyboardEvents()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.S:
                        PreviousRequested?.Invoke(this, EventArgs.Empty);
                        break;
                    case ConsoleKey.D:
                        NextRequested?.Invoke(this, EventArgs.Empty);
                        break;
                    case ConsoleKey.F:
                        EnterRequested?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
            Thread.Sleep(1000);
        }
    }
}
