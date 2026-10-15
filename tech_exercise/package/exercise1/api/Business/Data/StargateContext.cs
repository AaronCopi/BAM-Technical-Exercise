using Microsoft.EntityFrameworkCore;
using System.Data;

namespace StargateAPI.Business.Data
{
    public class StargateContext : DbContext
    {
        public IDbConnection Connection => Database.GetDbConnection();
        public DbSet<Person> People { get; set; }
        public DbSet<AstronautDetail> AstronautDetails { get; set; }
        public DbSet<AstronautDuty> AstronautDuties { get; set; }

        public StargateContext(DbContextOptions<StargateContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StargateContext).Assembly);
            modelBuilder.Entity<Person>().Property(p => p.Name).UseCollation("NOCASE");
            modelBuilder.Entity<AstronautDuty>().Property(ad => ad.DutyTitle).UseCollation("NOCASE");

            base.OnModelCreating(modelBuilder);
        }
    }
}
