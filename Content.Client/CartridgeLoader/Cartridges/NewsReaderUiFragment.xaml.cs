using Content.Client.Message;
using Content.Shared.MassMedia.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class NewsReaderUiFragment : BoxContainer
{
    public event Action? OnNextButtonPressed;
    public event Action? OnPrevButtonPressed;

    public event Action? OnNotificationSwithPressed;

    public NewsReaderUiFragment()
    {
        RobustXamlLoader.Load(this);
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        Next.OnPressed += _ => OnNextButtonPressed?.Invoke();
        Prev.OnPressed += _ => OnPrevButtonPressed?.Invoke();
        NotificationSwitch.OnPressed += _ => OnNotificationSwithPressed?.Invoke();
    }

    public void UpdateState(NewsArticle article, int targetNum, int totalNum, bool notificationOn)
    {
        PageNum.Visible = true;
        PageText.Visible = true;
        ShareTime.Visible = true;
        Author.Visible = true;

        PageName.Text = article.Name;
        PageText.SetMarkup(article.Content);

        PageNum.Text = $"{targetNum}/{totalNum}";

        NotificationSwitch.Text = Loc.GetString(notificationOn ? "news-read-ui-notification-on" : "news-read-ui-notification-off");

        string shareTime = article.ShareTime.ToString(@"hh\:mm\:ss");
        ShareTime.SetMarkup(Loc.GetString("news-read-ui-time-prefix-text") + " " + shareTime);

        Author.SetMarkup(Loc.GetString("news-read-ui-author-prefix") + " " + (article.Author != null ? article.Author : Loc.GetString("news-read-ui-no-author")));

        Prev.Disabled = targetNum <= 1;
        Next.Disabled = targetNum >= totalNum;
    }

    public void UpdateEmptyState(bool notificationOn)
    {
        PageNum.Visible = false;
        PageText.Visible = false;
        ShareTime.Visible = false;
        Author.Visible = false;

        PageName.Text = Loc.GetString("news-read-ui-not-found-text");

        NotificationSwitch.Text = Loc.GetString(notificationOn ? "news-read-ui-notification-on" : "news-read-ui-notification-off");
    }
}
