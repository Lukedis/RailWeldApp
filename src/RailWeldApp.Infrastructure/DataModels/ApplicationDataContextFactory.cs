using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailWeldApp.DataModels;

/// <summary>
/// 仅供 dotnet ef 设计时使用，不参与运行时 DI。
/// </summary>
public sealed class ApplicationDataContextFactory : IDesignTimeDbContextFactory<ApplicationDataContext>
{
    public ApplicationDataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDataContext>()
            .UseSqlite("Data Source=railweld.design.db")
            .Options;

        return new ApplicationDataContext(options);
    }
}
