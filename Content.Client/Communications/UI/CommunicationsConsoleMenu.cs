﻿using System.Threading;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleMenu : DefaultWindow
    {
        private CommunicationsConsoleBoundUserInterface Owner { get; set; }
        private readonly CancellationTokenSource _timerCancelTokenSource = new();
        private LineEdit _messageInput { get; set; }
        public readonly Button AnnounceButton;
        public readonly Button EmergencyShuttleButton;
        private readonly RichTextLabel _countdownLabel;
        public readonly OptionButton AlertLevelButton;

        public CommunicationsConsoleMenu(CommunicationsConsoleBoundUserInterface owner)
        {
            SetSize = MinSize = (600, 400);
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("communicationsconsole-menu-title");
            Owner = owner;

            _messageInput = new LineEdit
            {
                PlaceHolder = Loc.GetString("communicationsconsole-menu-announcement-placeholder"),
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 1
            };
            AnnounceButton = new Button();
            AnnounceButton.Text = "Announce";
            AnnounceButton.OnPressed += (_) => Owner.AnnounceButtonPressed(_messageInput.Text.Trim());
            AnnounceButton.Disabled = !owner.CanAnnounce;

            AlertLevelButton = new OptionButton();
            AlertLevelButton.OnItemSelected += args =>
            {
                var metadata = AlertLevelButton.GetItemMetadata(args.Id);
                if (metadata != null && metadata is string cast)
                {
                    Owner.AlertLevelSelected(cast);
                }
            };
            AlertLevelButton.Disabled = !owner.AlertLevelSelectable;

            _countdownLabel = new RichTextLabel(){MinSize = new Vector2(0, 200)};
            EmergencyShuttleButton = new Button();
            EmergencyShuttleButton.OnPressed += (_) => Owner.EmergencyShuttleButtonPressed();
            EmergencyShuttleButton.Disabled = !owner.CanCall;

            var vbox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            vbox.AddChild(_messageInput);
            vbox.AddChild(new Control(){MinSize = new Vector2(0,10), HorizontalExpand = true});
            vbox.AddChild(AnnounceButton);
            vbox.AddChild(AlertLevelButton);
            vbox.AddChild(new Control(){MinSize = new Vector2(0,10), HorizontalExpand = true});
            vbox.AddChild(_countdownLabel);
            vbox.AddChild(EmergencyShuttleButton);

            var hbox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true
            };
            hbox.AddChild(new Control(){MinSize = new Vector2(100,0), HorizontalExpand = true});
            hbox.AddChild(vbox);
            hbox.AddChild(new Control(){MinSize = new Vector2(100,0), HorizontalExpand = true});

            Contents.AddChild(hbox);

            UpdateCountdown();
            Timer.SpawnRepeating(1000, UpdateCountdown, _timerCancelTokenSource.Token);
        }

        // The current alert could make levels unselectable, so we need to ensure that the UI reacts properly.
        // If the current alert is unselectable, the only item in the alerts list will be
        // the current alert. Otherwise, it will be the list of alerts, with the current alert
        // selected.
        public void UpdateAlertLevels(List<string>? alerts, string currentAlert)
        {
            AlertLevelButton.Clear();

            if (alerts == null)
            {
                AlertLevelButton.AddItem(Loc.GetString($"alert-level-{currentAlert}"));
                AlertLevelButton.SetItemMetadata(AlertLevelButton.ItemCount - 1, currentAlert);
            }
            else
            {
                foreach (var alert in alerts)
                {
                     AlertLevelButton.AddItem(Loc.GetString($"alert-level-{alert}"));
                     AlertLevelButton.SetItemMetadata(AlertLevelButton.ItemCount - 1, alert);

                     if (alert == currentAlert)
                     {
                         AlertLevelButton.Select(AlertLevelButton.ItemCount - 1);
                     }
                }
            }
        }

        public void UpdateCountdown()
        {
            if (!Owner.CountdownStarted)
            {
                _countdownLabel.SetMessage("");
                EmergencyShuttleButton.Text = Loc.GetString("communicationsconsole-menu-call-shuttle");
                return;
            }

            EmergencyShuttleButton.Text = Loc.GetString("communicationsconsole-menu-recall-shuttle");
            _countdownLabel.SetMessage($"Time remaining\n{Owner.Countdown.ToString()}s");
        }

        public override void Close()
        {
            base.Close();

            _timerCancelTokenSource.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _timerCancelTokenSource.Cancel();
        }
    }
}
