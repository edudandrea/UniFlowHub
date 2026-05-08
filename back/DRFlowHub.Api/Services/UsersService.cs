using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Dtos;
using DRFlowHub.Api.Models;
using DRFlowHub.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace DRFlowHub.Api.Services
{
    public class UsersService
    {
        private readonly IUserRepo _repo;

        public UsersService(IUserRepo repo)
        {
            _repo = repo;
        }

        private static List<UserResponseDto> MapUsers(IQueryable<Users> query)
        {
            return query.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Cpf = u.Cpf,
                Email = u.Email,
                Role = u.Role,
                Departamento = u.Departamento,
                Cargo = u.Cargo,
                UnidadeId = u.UnidadeId,
                UnidadeNome = u.Unidade != null ? u.Unidade.Nome : string.Empty,
                DataNascimento = u.DataNascimento
            }).ToList();
        }

        public List<UserResponseDto> List(string role, string email)
        {
            IQueryable<Users> user = _repo.Query().Include(u => u.Unidade);

            if (RoleScope.IsAdmin(role) || RoleScope.IsTI(role) || RoleScope.IsRH(role))
                return MapUsers(user);

            user = user.Where(u => u.Email == email);
            return MapUsers(user);
        }

        public List<UserResponseDto> ListAdministradores(string role)
        {
            if (!RoleScope.IsAdmin(role) && !RoleScope.IsTI(role))
                throw new UnauthorizedAccessException("Voce nao pode listar administradores.");

            return MapUsers(_repo.Query()
                .Include(u => u.Unidade)
                .Where(u => u.Role == "Admin" || u.Role == "TI")
                .OrderBy(u => u.Nome));
        }

        public UserResponseDto Update(int id, UserUpdateDto dto, string role)
        {
            if (!RoleScope.IsAdmin(role) && !RoleScope.IsTI(role))
                throw new UnauthorizedAccessException("Voce nao pode editar usuarios.");

            var user = _repo.Query().Include(u => u.Unidade).FirstOrDefault(u => u.Id == id);
            if (user is null)
                throw new KeyNotFoundException("Usuario nao encontrado.");

            var email = dto.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new InvalidOperationException("Nome e obrigatorio.");

            if (string.IsNullOrWhiteSpace(dto.Cpf))
                throw new InvalidOperationException("CPF e obrigatorio.");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email e obrigatorio.");

            if (!AuthService.IsValidRole(dto.Role))
                throw new InvalidOperationException("Perfil invalido.");

            AuthService.ValidateUnidadeForRole(dto.Role, dto.UnidadeId);

            if (_repo.Query().Any(u => u.Id != id && u.Email == email))
                throw new InvalidOperationException("Ja existe um usuario com este email.");

            user.Nome = dto.Nome.Trim();
            user.Cpf = dto.Cpf.Trim();
            user.Email = email;
            user.Role = AuthService.NormalizeRole(dto.Role);
            user.Departamento = dto.Departamento.Trim();
            user.Cargo = dto.Cargo.Trim();
            user.UnidadeId = AuthService.ShouldRequireUnidade(dto.Role) ? dto.UnidadeId : null;
            user.DataNascimento = dto.DataNascimento;

            if (!string.IsNullOrWhiteSpace(dto.Senha))
            {
                if (dto.Senha.Length < 6)
                    throw new InvalidOperationException("Senha deve ter pelo menos 6 caracteres.");

                user.Senha = PasswordHasher.Hash(dto.Senha);
            }

            _repo.Update(user);
            _repo.Save();

            return MapUsers(_repo.Query().Include(u => u.Unidade).Where(u => u.Id == id)).Single();
        }

        public UserResponseDto UpdateProfile(int id, UserProfileUpdateDto dto)
        {
            var user = _repo.Query().Include(u => u.Unidade).FirstOrDefault(u => u.Id == id);
            if (user is null)
                throw new KeyNotFoundException("Usuario nao encontrado.");

            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new InvalidOperationException("Nome e obrigatorio.");

            if (string.IsNullOrWhiteSpace(dto.Cpf))
                throw new InvalidOperationException("CPF e obrigatorio.");

            user.Nome = dto.Nome.Trim();
            user.Cpf = dto.Cpf.Trim();
            user.Departamento = dto.Departamento.Trim();
            user.Cargo = dto.Cargo.Trim();
            user.DataNascimento = dto.DataNascimento;

            _repo.Update(user);
            _repo.Save();

            return MapUsers(_repo.Query().Include(u => u.Unidade).Where(u => u.Id == id)).Single();
        }

        public void ChangePassword(int id, UserChangePasswordDto dto)
        {
            var user = _repo.Query().FirstOrDefault(u => u.Id == id);
            if (user is null)
                throw new KeyNotFoundException("Usuario nao encontrado.");

            if (!PasswordHasher.Verify(dto.SenhaAtual, user.Senha))
                throw new UnauthorizedAccessException("Senha atual invalida.");

            if (string.IsNullOrWhiteSpace(dto.NovaSenha) || dto.NovaSenha.Length < 6)
                throw new InvalidOperationException("Nova senha deve ter pelo menos 6 caracteres.");

            user.Senha = PasswordHasher.Hash(dto.NovaSenha);
            _repo.Update(user);
            _repo.Save();
        }
    }
}
