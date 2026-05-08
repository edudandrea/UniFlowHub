using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DRFlowHub.Api.Data.Interfaces;
using DRFlowHub.Api.Dtos;
using DRFlowHub.Api.Dtos.Auth;
using DRFlowHub.Api.Models;
using DRFlowHub.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DRFlowHub.Api.Services
{
    public class AuthService
    {
        private readonly IUserRepo _repo;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepo repo, IConfiguration configuration)
        {
            _repo = repo;
            _configuration = configuration;
        }

        public LoginResponseDto Login(LoginRequestDto dto)
        {
            var user = _repo.GetByLogin(dto.Email.Trim());

            if (user is null || !PasswordHasher.Verify(dto.Senha, user.Senha))
                throw new UnauthorizedAccessException("Email ou senha invalidos.");

            return CreateLoginResponse(user);
        }

        public UserResponseDto CreateUser(UserCreateDto dto, int? createdByUserId)
        {
            ValidateUser(dto.Nome, dto.Cpf, dto.Email, dto.Senha, dto.Role);
            ValidateUnidadeForRole(dto.Role, dto.UnidadeId);

            if (_repo.Query().Any(u => u.Email == dto.Email.Trim()))
                throw new InvalidOperationException("Ja existe um usuario com este email.");

            if (_repo.Query().Any(u => u.Cpf == dto.Cpf.Trim()))
                throw new InvalidOperationException("Ja existe um usuario com este CPF.");

            var user = new Users
            {
                Nome = dto.Nome.Trim(),
                Cpf = dto.Cpf.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Senha = PasswordHasher.Hash(dto.Senha),
                Role = NormalizeRole(dto.Role),
                Departamento = dto.Departamento.Trim(),
                Cargo = dto.Cargo.Trim(),
                UnidadeId = ShouldRequireUnidade(dto.Role) ? dto.UnidadeId : null,
                DataNascimento = dto.DataNascimento,
                CreatedByUserId = createdByUserId
            };

            _repo.Add(user);
            _repo.Save();

            return ToResponse(_repo.Query().Include(u => u.Unidade).First(u => u.Id == user.Id));
        }

        public bool HasAnyUser()
        {
            return _repo.HasAnyUser();
        }

        private LoginResponseDto CreateLoginResponse(Users user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int?>("Jwt:ExpiresMinutes") ?? 480);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Nome),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, NormalizeRole(user.Role))
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                User = ToResponse(user)
            };
        }

        private static void ValidateUser(string nome, string cpf, string email, string senha, string role)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome e obrigatorio.");

            if (string.IsNullOrWhiteSpace(cpf))
                throw new InvalidOperationException("CPF e obrigatorio.");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email e obrigatorio.");

            if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6)
                throw new InvalidOperationException("Senha deve ter pelo menos 6 caracteres.");

            if (!IsValidRole(role))
                throw new InvalidOperationException("Perfil invalido.");
        }

        public static bool IsValidRole(string role)
        {
            return RoleScope.IsAdmin(role)
                || RoleScope.IsRH(role)
                || RoleScope.IsTI(role)
                || RoleScope.IsDiretoria(role)
                || RoleScope.IsCompras(role)
                || RoleScope.IsGerente(role)
                || RoleScope.IsUser(role);
        }

        public static string NormalizeRole(string role)
        {
            if (RoleScope.IsAdmin(role)) return "Admin";
            if (RoleScope.IsRH(role)) return "RH";
            if (RoleScope.IsTI(role)) return "TI";
            if (RoleScope.IsDiretoria(role)) return "Diretoria";
            if (RoleScope.IsCompras(role)) return "Compras";
            if (RoleScope.IsGerente(role)) return "Gestor";
            return "Usuario";
        }

        public static bool ShouldRequireUnidade(string role)
        {
            return RoleScope.IsGerente(role) || RoleScope.IsUser(role);
        }

        public static void ValidateUnidadeForRole(string role, int? unidadeId)
        {
            if (ShouldRequireUnidade(role) && (!unidadeId.HasValue || unidadeId.Value <= 0))
                throw new InvalidOperationException("Unidade e obrigatoria para perfis Gestor e Usuario.");
        }

        public static UserResponseDto ToResponse(Users user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Nome = user.Nome,
                Cpf = user.Cpf,
                Email = user.Email,
                Role = NormalizeRole(user.Role),
                Departamento = user.Departamento,
                Cargo = user.Cargo,
                UnidadeId = user.UnidadeId,
                UnidadeNome = user.Unidade?.Nome ?? string.Empty,
                DataNascimento = user.DataNascimento
            };
        }
    }
}
