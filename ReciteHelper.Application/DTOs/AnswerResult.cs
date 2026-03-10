using ReciteHelper.Core.Entities;

namespace ReciteHelper.Application.DTOs;

internal class AnswerResult
{
    public bool IsCorrect { get; set; }
    public int QValue { get; set; }
    public double NewEFValue { get; set; }
    public required ReviewTag ReviewTag { get; set; }
    public double RRelative { get; set; }
}
