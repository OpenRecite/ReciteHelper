using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Utilities;

namespace ReciteHelper.Infrastructure.Services;

public sealed class StartupCompatibilityService : IStartupCompatibilityService
{
    public void Initialize()
    {
        Deformity.HorribleMethod();
    }
}
