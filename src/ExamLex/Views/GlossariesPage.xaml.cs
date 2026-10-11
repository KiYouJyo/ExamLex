using System.Collections.ObjectModel;
using System.Text.Json;
using ExamLex.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLex.Views;

/// <summary>Flint3 GlossariesPage layout, with user-local ExamLex vocabulary collections.</summary>
public sealed partial class GlossariesPage : Page
{
    public ObservableCollection<GlossaryMyModel> MyGlossaries { get; } = new();
    private Action? _backHome;
    private Action<GlossaryMyModel>? _openWords;
    private readonly string _file = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExamLex","glossaries.json");

    public GlossariesPage() => InitializeComponent();

    public void Initialize(IEnumerable<string> existingFavorites,Action backHome,
        Action<GlossaryMyModel> openWords)
    {
        _backHome=backHome;
        _openWords=openWords;
        MyGlossaries.Clear();
        try
        {
            if (File.Exists(_file))
            {
                foreach (var item in JsonSerializer.Deserialize<List<GlossaryMyModel>>(File.ReadAllText(_file)) ?? [])
                    MyGlossaries.Add(item);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException) { }
        if (existingFavorites.Any() && !MyGlossaries.Any(w=>w.GlossaryTitle=="我的生词本"))
            MyGlossaries.Add(new GlossaryMyModel{GlossaryTitle="我的生词本",
                GlossaryDescription="已收藏的单词"});
        RefreshEmpty();
    }

    private void RefreshEmpty()
    {
        NoCollectionsState.Visibility=MyGlossaries.Count==0?Visibility.Visible:Visibility.Collapsed;
        AddGlossaryButton.Visibility=GlossaryTabSegmented.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;
        WarningButton.Visibility=GlossaryTabSegmented.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;
    }

    private void OnTabChanged(object sender,SelectionChangedEventArgs e)
    {
        if(MyGlossariesGrid is null || ExGlossariesGrid is null)return;
        bool my=GlossaryTabSegmented.SelectedIndex==0;
        MyGlossariesGrid.Visibility=my?Visibility.Visible:Visibility.Collapsed;
        ExGlossariesGrid.Visibility=my?Visibility.Collapsed:Visibility.Visible;
        RefreshEmpty();
    }

    private void OnBackClicked(object sender,RoutedEventArgs e)=>_backHome?.Invoke();

    private void OnClickGoMyGlossary(object sender,RoutedEventArgs e)
    {
        if((sender as Button)?.DataContext is GlossaryMyModel selected)
            _openWords?.Invoke(selected);
    }
    private void OnCreateGlossaryClicked(object sender,RoutedEventArgs e)
    {
        string title=AddGlossaryNameTextBox.Text.Trim();
        if(title.Length==0 || title.Length>48 || MyGlossaries.Any(g=>g.GlossaryTitle==title))
            return;
        MyGlossaries.Add(new GlossaryMyModel{GlossaryTitle=title,
            GlossaryDescription=AddGlossaryDescTextBox.Text.Trim()});
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        File.WriteAllText(_file,JsonSerializer.Serialize(MyGlossaries.ToArray()));
        AddGlossaryFlyout.Hide();
        AddGlossaryNameTextBox.Text="";
        AddGlossaryDescTextBox.Text="";
        RefreshEmpty();
    }
}
