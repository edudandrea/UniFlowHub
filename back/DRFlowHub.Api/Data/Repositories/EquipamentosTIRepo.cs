using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Repositories
{
    public class EquipamentosTIRepo : BaseRepo<EquipamentoTI>, IEquipamentosTIRepo
    {
        public EquipamentosTIRepo(AppDbContext context) : base(context) { }
    }
}
