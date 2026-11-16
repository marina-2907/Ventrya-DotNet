
using Microsoft.EntityFrameworkCore;
using Ventrya.Api.Models;

namespace Ventrya.Api.Data;

public class VentryaDbContext : DbContext
{
    public VentryaDbContext(
        DbContextOptions<VentryaDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    public DbSet<RegistroIdempotencia> RegistrosIdempotencia
        => Set<RegistroIdempotencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroIdempotencia>()
            .HasKey(r => r.Chave);
    }
}
