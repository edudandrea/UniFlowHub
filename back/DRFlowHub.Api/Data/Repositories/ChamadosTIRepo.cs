using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Repositories
{
    public class ChamadosTIRepo : BaseRepo<ChamadosTI>, IChamadosTIRepo
    {
        public ChamadosTIRepo(AppDbContext context) : base(context) { }

        public IQueryable<ChamadoTIComunicacao> QueryComunicacoes()
        {
            return _context.ChamadoTIComunicacao;
        }

        public void AddComunicacao(ChamadoTIComunicacao entity)
        {
            _context.ChamadoTIComunicacao.Add(entity);
        }
    }
}
