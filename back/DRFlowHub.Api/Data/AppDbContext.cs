using DRFlowHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DRFlowHub.Api.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Users> User { get; set; }
        public DbSet<SolicitacoesRH> SolicitacaoRH { get; set; }
        public DbSet<SolicitacaoRHComunicacao> SolicitacaoRHComunicacao { get; set; }
        public DbSet<ChamadosTI> ChamadoTI { get; set; }
        public DbSet<ChamadoTIComunicacao> ChamadoTIComunicacao { get; set; }
        public DbSet<EquipamentoTI> EquipamentoTI { get; set; }
        public DbSet<SolicitacaoCompra> SolicitacaoCompra { get; set; }
        public DbSet<SolicitacaoCompraComunicacao> SolicitacaoCompraComunicacao { get; set; }
        public DbSet<Unidade> Unidade { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.CreatedByUser)
                .WithMany(u => u.CreatedUsers)
                .HasForeignKey(u => u.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.Unidade)
                .WithMany(u => u.Usuarios)
                .HasForeignKey(u => u.UnidadeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SolicitacoesRH>()
                .HasOne(s => s.OwnerUser)
                .WithMany()
                .HasForeignKey(s => s.Userid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SolicitacaoRHComunicacao>()
                .HasOne(s => s.SolicitacaoRH)
                .WithMany(s => s.Comunicacoes)
                .HasForeignKey(s => s.SolicitacaoRHId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChamadosTI>()
                .HasOne(s => s.OwnerUser)
                .WithMany()
                .HasForeignKey(s => s.Userid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChamadoTIComunicacao>()
                .HasOne(s => s.ChamadoTI)
                .WithMany(s => s.Comunicacoes)
                .HasForeignKey(s => s.ChamadoTIId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EquipamentoTI>()
                .HasOne(s => s.OwnerUser)
                .WithMany()
                .HasForeignKey(s => s.Userid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SolicitacaoCompra>()
                .HasOne(s => s.OwnerUser)
                .WithMany()
                .HasForeignKey(s => s.Userid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SolicitacaoCompraComunicacao>()
                .HasOne(s => s.SolicitacaoCompra)
                .WithMany(s => s.Comunicacoes)
                .HasForeignKey(s => s.SolicitacaoCompraId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
