using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Interfaces
{
    public interface IChamadosTIRepo : IBaseRepo<ChamadosTI>
    {
        IQueryable<ChamadoTIComunicacao> QueryComunicacoes();
        void AddComunicacao(ChamadoTIComunicacao entity);
    }
}
