using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Interfaces
{
    public interface ISolicitacoesCompraRepo : IBaseRepo<SolicitacaoCompra>
    {
        IQueryable<SolicitacaoCompraComunicacao> QueryComunicacoes();
        void AddComunicacao(SolicitacaoCompraComunicacao entity);
    }
}
