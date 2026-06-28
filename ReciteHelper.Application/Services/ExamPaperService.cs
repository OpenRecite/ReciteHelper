using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Application.Services;

public sealed class ExamPaperService : IExamPaperService
{
    private const double ChoiceQuestionRatio = 0.30d;

    public List<Question> Generate(Project project, ExamSettings settings)
    {
        var candidates = project.Chapters?
            .Where(chapter => chapter.Questions is { Count: > 0 })
            .SelectMany(chapter => chapter.Questions!.Select(question => new QuestionCandidate(
                question,
                chapter.Name ?? string.Empty,
                GetChapterWeight(settings, chapter.Name))))
            .ToList() ?? [];

        if (candidates.Count < settings.QuestionCount)
            return [];

        var choices = candidates.Where(candidate => candidate.Question.IsSingleChoice).ToList();
        var shortAnswers = candidates.Where(candidate => !candidate.Question.IsSingleChoice).ToList();
        var targetChoiceCount = Math.Min(
            choices.Count,
            (int)Math.Round(settings.QuestionCount * ChoiceQuestionRatio, MidpointRounding.AwayFromZero));
        var targetShortAnswerCount = Math.Min(shortAnswers.Count, settings.QuestionCount - targetChoiceCount);

        var missingCount = settings.QuestionCount - targetChoiceCount - targetShortAnswerCount;
        if (missingCount > 0)
        {
            var remainingChoices = choices.Count - targetChoiceCount;
            var choiceSupplement = Math.Min(remainingChoices, missingCount);
            targetChoiceCount += choiceSupplement;
            missingCount -= choiceSupplement;

            targetShortAnswerCount += Math.Min(
                shortAnswers.Count - targetShortAnswerCount,
                missingCount);
        }

        var selectedChoices = SelectCandidates(choices, targetChoiceCount);
        var selectedShortAnswers = SelectCandidates(shortAnswers, targetShortAnswerCount);

        return selectedChoices
            .Concat(selectedShortAnswers)
            .Select(candidate => CloneQuestion(candidate.Question))
            .ToList();
    }

    private static double GetChapterWeight(ExamSettings settings, string? chapterName)
    {
        if (chapterName is not null &&
            settings.ChapterWeights?.TryGetValue(chapterName, out var weight) is true)
        {
            return Math.Max(0d, weight);
        }

        return 0d;
    }

    private static List<QuestionCandidate> SelectCandidates(
        IReadOnlyCollection<QuestionCandidate> candidates,
        int count)
    {
        if (count <= 0)
            return [];

        var chapterCandidateCounts = candidates
            .GroupBy(candidate => candidate.ChapterName)
            .ToDictionary(group => group.Key, group => group.Count());
        var positiveWeightCandidates = candidates
            .Where(candidate => candidate.ChapterWeight > 0d)
            .OrderByDescending(candidate => CreateWeightedRandomKey(
                candidate.ChapterWeight / chapterCandidateCounts[candidate.ChapterName]))
            .Take(count)
            .ToList();

        if (positiveWeightCandidates.Count >= count)
            return positiveWeightCandidates;

        var selectedQuestions = positiveWeightCandidates
            .Select(candidate => candidate.Question)
            .ToHashSet();
        var supplements = candidates
            .Where(candidate => !selectedQuestions.Contains(candidate.Question))
            .OrderBy(_ => Random.Shared.Next())
            .Take(count - positiveWeightCandidates.Count);

        positiveWeightCandidates.AddRange(supplements);
        return positiveWeightCandidates;
    }

    private static double CreateWeightedRandomKey(double weight)
    {
        var random = Math.Max(double.Epsilon, Random.Shared.NextDouble());
        return Math.Pow(random, 1d / weight);
    }

    private static Question CloneQuestion(Question question)
    {
        return new Question
        {
            Text = question.Text,
            Type = question.Type,
            Options = question.Options
                .Select(option => new ReciteHelper.Core.ValueObjects.QuestionOption
                {
                    Id = option.Id,
                    Text = option.Text
                })
                .ToList(),
            CorrectOptionIds = question.CorrectOptionIds.ToList(),
            CorrectAnswer = question.CorrectAnswer,
        };
    }

    private sealed record QuestionCandidate(
        Question Question,
        string ChapterName,
        double ChapterWeight);
}
