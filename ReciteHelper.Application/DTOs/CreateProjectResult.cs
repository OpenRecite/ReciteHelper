using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Application.DTOs;

public sealed record CreateProjectResult(Project Project, string ProjectPath);
