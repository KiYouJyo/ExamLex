using ExamLex.Models;
using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLex.Views;

public sealed partial class GlossaryWordsPage : Page
{
    private Action? _back;
    private readonly List<string> _words = new();
    private string _description="";
    private bool _reverse;

    public GlossaryWordsPage() { InitializeComponent(); MicaSurfaceService.Attach(this); }

    public void Initialize(GlossaryMyModel glossary,IEnumerable<string> words,Action back)
    {
        _back=back;
        GlossaryTitle.Text=glossary.GlossaryTitle;
        _description=glossary.GlossaryDescription;
        _words.Clear();
        if(glossary.GlossaryTitle=="我的生词本")_words.AddRange(words);
        Render();
    }

    private void Render()
    {
        string query=FilterWordTextBox.Text.Trim();
        var filtered=_words.Where(w=>w.Contains(query,StringComparison.OrdinalIgnoreCase))
            .OrderBy(w=>w,StringComparer.OrdinalIgnoreCase).ToArray();
        if(_reverse)Array.Reverse(filtered);
        GlossaryWordsListView.ItemsSource=filtered;
        NoWordsState.Visibility=filtered.Length==0?Visibility.Visible:Visibility.Collapsed;
        GlossaryWordsListView.Visibility=filtered.Length==0?Visibility.Collapsed:Visibility.Visible;
    }
    private void OnFilterChanged(object sender, TextChangedEventArgs e)
    {
        if(NoWordsState is not null)Render();
    }
    private void OnBackClicked(object sender,RoutedEventArgs e)=>_back?.Invoke();
    private void OnSortClicked(object sender,RoutedEventArgs e){_reverse=!_reverse;Render();}
    private async void OnPropertiesClicked(object sender,RoutedEventArgs e)
    {
        await MicaSurfaceService.ShowAsync(new ContentDialog
        {
            XamlRoot=XamlRoot,Title="生词本属性",
            Content=new TextBlock{Text=$"{GlossaryTitle.Text}\n{_description}\n共 {_words.Count} 个单词",
                TextWrapping=TextWrapping.Wrap},
            CloseButtonText="返回"
        });
    }
}
