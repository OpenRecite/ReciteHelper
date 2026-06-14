namespace ReciteHelper.Application.DTOs;

public sealed record ProjectCreationProgress(
    int ScanCurrent,
    int ScanTotal,
    int ClusterCurrent,
    int ClusterTotal,
    int RoundCurrent,
    int RoundTotal,
    string? Label = null);
