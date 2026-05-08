using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DRFlowHub.Api.Data.Repositories;
using DRFlowHub.Api.Models;

namespace DRFlowHub.Api.Data.Interfaces
{
    public interface IUserRepo : IBaseRepo<Users>
    {
         Users? GetByLogin(string login);
         bool HasAnyUser();
    }
}