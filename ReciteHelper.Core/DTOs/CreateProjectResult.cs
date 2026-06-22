using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Core.DTOs;

public sealed record CreateProjectResult(Project Project, string ProjectPath);
