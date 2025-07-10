using Microsoft.EntityFrameworkCore;
using AwsHelper.Models;

namespace AwsHelper.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<ParameterStorage> ParameterStorages => Set<ParameterStorage>();
}