using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MachineLinking.UI
{
    /// <summary>
    /// Initializes a <see cref="SignalTimerWindow"/> and updates it when new server messages are received.
    /// Adapted from HandLabeler code by rolfero and Watermelon914 on GitHub.
    /// </summary>
    public sealed class SignalTimerBoundUserInterface : BoundUserInterface
    {
        private SignalTimerWindow? _window;

        public SignalTimerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new SignalTimerWindow(this);
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnTimeEntered += OnTimeChanged;

        }

        private void OnTimeChanged(float newTime)
        {
            SendMessage(new SignalTimerLengthChangedMessage(newTime));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not SignalTimerState cast)
                return;

            _window.SetCurrentTime(cast.Length.ToString());
            _window.HandleState(cast.State, cast.Remaining);
        }

        public void OnStart()
        {
            SendMessage(new SignalTimerStartedMessage());
        }

        public void OnCancel()
        {
            SendMessage(new SignalTimerCancelledMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
