using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Repositories
{
    public class UnidadesRepo : BaseRepo<Unidade>, IUnidadesRepo
    {
        public UnidadesRepo(AppDbContext context) : base(context)
        {
        }
    }
}
