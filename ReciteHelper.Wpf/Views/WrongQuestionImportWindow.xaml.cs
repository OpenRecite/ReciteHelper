using ReciteHelper.Wpf.Models;
using System.Windows;

namespace ReciteHelper.Wpf.Views;

public partial class WrongQuestionImportWindow : Window
{
    private readonly List<WrongQuestionImportCandidate> _candidates;

    public WrongQuestionImportWindow(List<WrongQuestionImportCandidate> candidates)
    {
        InitializeComponent();
        _candidates = candidates;
        CandidatesItemsControl.ItemsSource = _candidates;

        var duplicateCount = _candidates.Count(candidate => candidate.HasSimilarQuestion);
        SummaryText.Text = duplicateCount == 0
            ? $"共发现 {_candidates.Count} 道错题，已默认全选。确认后会归入已有章节并更新知识库。"
            : $"共发现 {_candidates.Count} 道错题，其中 {duplicateCount} 道与题库已有题目高度相似，已默认取消勾选。确认后会归入已有章节并更新知识库。";
    }

    public IReadOnlyList<WrongQuestionImportCandidate> SelectedCandidates =>
        _candidates.Where(candidate => candidate.IsSelected).ToList();

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var candidate in _candidates)
            candidate.IsSelected = true;
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var candidate in _candidates)
            candidate.IsSelected = false;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (!SelectedCandidates.Any())
        {
            MessageBox.Show("请至少选择一道错题。", "未选择题目", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
