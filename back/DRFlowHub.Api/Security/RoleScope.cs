
namespace DRFlowHub.Api.Security
{
    public static class RoleScope
    {
        
        public static bool IsAdmin(string? role)
            => string.Equals(role?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);

        public static bool IsRH(string? role)
            => string.Equals(role?.Trim(), "RH", StringComparison.OrdinalIgnoreCase);

        public static bool IsTI(string? role)
            => string.Equals(role?.Trim(), "TI", StringComparison.OrdinalIgnoreCase);

        public static bool IsDiretoria(string? role)
            => string.Equals(role?.Trim(), "Diretoria", StringComparison.OrdinalIgnoreCase);

        public static bool IsCompras(string? role)
            => string.Equals(role?.Trim(), "Compras", StringComparison.OrdinalIgnoreCase);

        public static bool IsGerente(string? role)
            => string.Equals(role?.Trim(), "Gerente", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role?.Trim(), "Gestor", StringComparison.OrdinalIgnoreCase);

            public static bool IsUser(string? role)
            => string.Equals(role?.Trim(), "Usuario", StringComparison.OrdinalIgnoreCase);        

        
    }
}
