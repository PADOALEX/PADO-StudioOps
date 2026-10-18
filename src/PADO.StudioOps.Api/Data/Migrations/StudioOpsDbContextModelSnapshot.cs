using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PADO.StudioOps.Api.Data.Migrations;

[DbContext(typeof(StudioOpsDbContext))]
public sealed class StudioOpsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => StudioOpsDbContext.ConfigureModel(modelBuilder);
}
