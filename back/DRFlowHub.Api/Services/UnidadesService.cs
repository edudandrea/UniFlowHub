using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Dtos.Unidades;
using DRFlowHub.Api.Models;
using DRFlowHub.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace DRFlowHub.Api.Services
{
    public class UnidadesService
    {
        private readonly IUnidadesRepo _repo;

        public UnidadesService(IUnidadesRepo repo)
        {
            _repo = repo;
        }

        public List<UnidadeResponseDto> List()
        {
            return _repo.Query()
                .AsNoTracking()
                .OrderBy(u => u.Nome)
                .Select(u => MapResponse(u))
                .ToList();
        }

        public UnidadeResponseDto Add(UnidadeCreateDto dto, string role)
        {
            EnsureCanManage(role);
            Validate(dto);

            var cnpj = NormalizeCnpj(dto.Cnpj);
            if (_repo.Query().Any(u => u.Cnpj == cnpj))
                throw new InvalidOperationException("Ja existe uma unidade com este CNPJ.");

            var unidade = new Unidade
            {
                Nome = dto.Nome.Trim(),
                Cnpj = cnpj,
                Endereco = dto.Endereco.Trim(),
                DataCadastro = DateTime.UtcNow
            };

            _repo.Add(unidade);
            _repo.Save();

            return MapResponse(unidade);
        }

        public UnidadeResponseDto Update(int id, UnidadeCreateDto dto, string role)
        {
            EnsureCanManage(role);
            Validate(dto);

            var unidade = _repo.Query().FirstOrDefault(u => u.Id == id);
            if (unidade is null)
                throw new KeyNotFoundException("Unidade nao encontrada.");

            var cnpj = NormalizeCnpj(dto.Cnpj);
            if (_repo.Query().Any(u => u.Id != id && u.Cnpj == cnpj))
                throw new InvalidOperationException("Ja existe uma unidade com este CNPJ.");

            unidade.Nome = dto.Nome.Trim();
            unidade.Cnpj = cnpj;
            unidade.Endereco = dto.Endereco.Trim();

            _repo.Update(unidade);
            _repo.Save();

            return MapResponse(unidade);
        }

        private static void EnsureCanManage(string role)
        {
            if (!RoleScope.IsAdmin(role) && !RoleScope.IsRH(role))
                throw new UnauthorizedAccessException("Voce nao pode administrar unidades.");
        }

        private static void Validate(UnidadeCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new InvalidOperationException("Nome da unidade e obrigatorio.");

            if (string.IsNullOrWhiteSpace(dto.Cnpj))
                throw new InvalidOperationException("CNPJ e obrigatorio.");

            if (string.IsNullOrWhiteSpace(dto.Endereco))
                throw new InvalidOperationException("Endereco e obrigatorio.");
        }

        private static string NormalizeCnpj(string cnpj)
        {
            return new string(cnpj.Where(char.IsDigit).ToArray());
        }

        private static UnidadeResponseDto MapResponse(Unidade unidade)
        {
            return new UnidadeResponseDto
            {
                Id = unidade.Id,
                Nome = unidade.Nome,
                Cnpj = unidade.Cnpj,
                Endereco = unidade.Endereco,
                DataCadastro = unidade.DataCadastro
            };
        }
    }
}
