using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DRFlowHub.Api.Data.Repositories;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Interfaces
{
    public interface ISolicitacoesRHRepo : IBaseRepo<SolicitacoesRH>
    {
        IQueryable<SolicitacaoRHComunicacao> QueryComunicacoes();
        void AddComunicacao(SolicitacaoRHComunicacao entity);
    }
}
