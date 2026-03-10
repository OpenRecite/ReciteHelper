using System;
using System.Collections.Generic;
using System.Text;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IAnswerJudge
{
    Task<bool> JudgeAsync(string? userAnswer, string? correctAnswer);

    Task<double> CalculateSimilarityAsync(string userAnswer, string correctAnswer);
}
