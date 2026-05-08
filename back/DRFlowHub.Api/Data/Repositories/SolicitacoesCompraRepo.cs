using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Repositories
{
    public class SolicitacoesCompraRepo : BaseRepo<SolicitacaoCompra>, ISolicitacoesCompraRepo
    {
        public SolicitacoesCompraRepo(AppDbContext context) : base(context) { }

        public IQueryable<SolicitacaoCompraComunicacao> QueryComunicacoes()
        {
            return _context.SolicitacaoCompraComunicacao;
        }

        public void AddComunicacao(SolicitacaoCompraComunicacao entity)
        {
            _context.SolicitacaoCompraComunicacao.Add(entity);
        }
    }
}
