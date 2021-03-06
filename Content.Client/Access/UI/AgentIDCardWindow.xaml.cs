using Robust.Client.UserInterface.CustomControls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Access.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class AgentIDCardWindow : DefaultWindow
    {
        public event Action<string>? OnNameEntered;

        public event Action<string>? OnJobEntered;

        public AgentIDCardWindow()
        {
            RobustXamlLoader.Load(this);

            NameLineEdit.OnTextEntered += e => OnNameEntered?.Invoke(e.Text);
            JobLineEdit.OnTextEntered += e => OnJobEntered?.Invoke(e.Text);
        }

        public void SetCurrentName(string name)
        {
            NameLineEdit.Text = name;
        }

        public void SetCurrentJob(string job)
        {
            JobLineEdit.Text = job;
        }
    }
}
