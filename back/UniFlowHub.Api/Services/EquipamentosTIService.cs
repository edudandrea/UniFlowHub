using UniFlowHub.Api.Data.Interfaces;
using UniFlowHub.Api.Dtos.EquipamentosTI;
using UniFlowHub.Api.Models;
using UniFlowHub.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace UniFlowHub.Api.Services
{
    public class EquipamentosTIService
    {
        private readonly IEquipamentosTIRepo _repo;

        public EquipamentosTIService(IEquipamentosTIRepo repo)
        {
            _repo = repo;
        }

        public List<EquipamentoTIResponseDto> List(string role)
        {
            EnsureCanManage(role);

            return _repo.Query()
                .AsNoTracking()
                .OrderByDescending(s => s.DataMovimentacao)
                .Select(s => MapResponse(s))
                .ToList();
        }

        public EquipamentoTIResponseDto Add(EquipamentoTICreateDto dto, string role, int userId, string documentoUrl)
        {
            EnsureCanManage(role);
            Validate(dto.Tipo, dto.Patrimonio, dto.Responsavel);

            var equipamento = new EquipamentoTI
            {
                Tipo = dto.Tipo.Trim(),
                Patrimonio = dto.Patrimonio.Trim(),
                Modelo = dto.Modelo.Trim(),
                Serial = dto.Serial.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Enviado" : dto.Status.Trim(),
                Origem = dto.Origem.Trim(),
                Destino = dto.Destino.Trim(),
                Responsavel = dto.Responsavel.Trim(),
                DataMovimentacao = DateTime.UtcNow,
                DataPrevistaRetorno = dto.DataPrevistaRetorno,
                Observacoes = dto.Observacoes.Trim(),
                DocumentoUrl = documentoUrl,
                Userid = userId
            };

            _repo.Add(equipamento);
            _repo.Save();

            return MapResponse(equipamento);
        }

        public EquipamentoTIResponseDto Update(int id, EquipamentoTIUpdateDto dto, string role)
        {
            EnsureCanManage(role);
            Validate(dto.Tipo, dto.Patrimonio, dto.Responsavel);

            var equipamento = _repo.Query().FirstOrDefault(s => s.Id == id);
            if (equipamento is null)
                throw new KeyNotFoundException("Equipamento nao encontrado.");

            equipamento.Tipo = dto.Tipo.Trim();
            equipamento.Patrimonio = dto.Patrimonio.Trim();
            equipamento.Modelo = dto.Modelo.Trim();
            equipamento.Serial = dto.Serial.Trim();
            equipamento.Status = dto.Status.Trim();
            equipamento.Origem = dto.Origem.Trim();
            equipamento.Destino = dto.Destino.Trim();
            equipamento.Responsavel = dto.Responsavel.Trim();
            equipamento.DataPrevistaRetorno = dto.DataPrevistaRetorno;
            equipamento.Observacoes = dto.Observacoes.Trim();

            _repo.Update(equipamento);
            _repo.Save();

            return MapResponse(equipamento);
        }

        public EquipamentoTI GetAttachmentOwner(int id, string role)
        {
            EnsureCanManage(role);

            var equipamento = _repo.Query().AsNoTracking().FirstOrDefault(s => s.Id == id);
            if (equipamento is null)
                throw new KeyNotFoundException("Equipamento nao encontrado.");

            if (string.IsNullOrWhiteSpace(equipamento.DocumentoUrl))
                throw new FileNotFoundException("Este registro nao possui documento.");

            return equipamento;
        }

        private static void EnsureCanManage(string role)
        {
            if (!RoleScope.IsAdmin(role) && !RoleScope.IsTI(role))
                throw new UnauthorizedAccessException("Somente TI pode acessar o controle de equipamentos.");
        }

        private static void Validate(string tipo, string patrimonio, string responsavel)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                throw new InvalidOperationException("Tipo e obrigatorio.");

            if (string.IsNullOrWhiteSpace(patrimonio))
                throw new InvalidOperationException("Patrimonio e obrigatorio.");

            if (string.IsNullOrWhiteSpace(responsavel))
                throw new InvalidOperationException("Responsavel e obrigatorio.");
        }

        private static EquipamentoTIResponseDto MapResponse(EquipamentoTI s)
        {
            return new EquipamentoTIResponseDto
            {
                Id = s.Id,
                Tipo = s.Tipo,
                Patrimonio = s.Patrimonio,
                Modelo = s.Modelo,
                Serial = s.Serial,
                Status = s.Status,
                Origem = s.Origem,
                Destino = s.Destino,
                Responsavel = s.Responsavel,
                DataMovimentacao = s.DataMovimentacao,
                DataPrevistaRetorno = s.DataPrevistaRetorno,
                Observacoes = s.Observacoes,
                DocumentoUrl = s.DocumentoUrl,
                Userid = s.Userid
            };
        }
    }
}
