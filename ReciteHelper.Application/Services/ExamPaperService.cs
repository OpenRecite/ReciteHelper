using ReciteHelper.Application.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Application.Services;

public sealed class ExamPaperService : IExamPaperService
{
    public List<Question> Generate(Project project, ExamSettings settings)
    {
        var allQuestions = project.Chapters?
            .Where(chapter => chapter.Questions is not null)
            .SelectMany(chapter => chapter.Questions!)
            .Select(CloneQuestion)
            .ToList() ?? [];

        if (allQuestions.Count < settings.QuestionCount)
            return [];

        if (settings.ChapterWeights is null || settings.ChapterWeights.All(weight => weight.Value == 0))
            return GetRandomQuestions(allQuestions, settings.QuestionCount);

        return GetWeightedQuestions(project, settings);
    }

    private static List<Question> GetRandomQuestions(List<Question> allQuestions, int count)
    {
        return allQuestions.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
    }

    private static List<Question> GetWeightedQuestions(Project project, ExamSettings settings)
    {
        var selectedQuestions = new List<Question>();
        var weights = settings.ChapterWeights!;
        var totalWeight = weights.Values.Sum();

        if (totalWeight <= 0)
            return [];

        foreach (var chapter in project.Chapters ?? [])
        {
            if (chapter.Name is null ||
                !weights.TryGetValue(chapter.Name, out var weight) ||
                weight == 0 ||
                chapter.Questions is null)
            {
                continue;
            }

            var proportion = weight / totalWeight;
            var chapterQuestionCount = (int)Math.Round(settings.QuestionCount * proportion);
            chapterQuestionCount = Math.Max(1, Math.Min(chapterQuestionCount, chapter.Questions.Count));

            selectedQuestions.AddRange(
                chapter.Questions
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(chapterQuestionCount)
                    .Select(CloneQuestion));
        }

        if (selectedQuestions.Count < settings.QuestionCount)
        {
            var remainingCount = settings.QuestionCount - selectedQuestions.Count;
            var selectedTexts = selectedQuestions.Select(question => question.Text).ToHashSet();
            var remainingQuestions = project.Chapters?
                .Where(chapter => chapter.Questions is not null)
                .SelectMany(chapter => chapter.Questions!)
                .Where(question => !selectedTexts.Contains(question.Text))
                .OrderBy(_ => Random.Shared.Next())
                .Take(remainingCount)
                .Select(CloneQuestion) ?? [];

            selectedQuestions.AddRange(remainingQuestions);
        }

        return selectedQuestions
            .OrderBy(_ => Random.Shared.Next())
            .Take(settings.QuestionCount)
            .ToList();
    }

    private static Question CloneQuestion(Question question)
    {
        return new Question
        {
            Text = question.Text,
            CorrectAnswer = question.CorrectAnswer,
        };
    }
}
