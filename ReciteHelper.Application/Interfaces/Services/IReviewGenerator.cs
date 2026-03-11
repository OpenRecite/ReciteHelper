using ReciteHelper.Application.DTOs;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IReviewGenerator
{
    List<Question> GenerateReview(Project project, int count);
    List<Question> GenerateParameterizationReview(Project project, ReviewOptions options);
}
