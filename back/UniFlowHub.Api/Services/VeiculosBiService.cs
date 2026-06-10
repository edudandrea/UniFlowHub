using System.Data;
using System.Data.Common;
using System.Globalization;
using UniFlowHub.Api.Data;
using UniFlowHub.Api.Dtos.VeiculosBi;
using UniFlowHub.Api.Security;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace UniFlowHub.Api.Services
{
    public class VeiculosBiService
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;

        private const string AcessoriosSql = @"
            SELECT *
            FROM (
                SELECT
                    COALESCE(TO_CHAR(PIE.ITEM_ESTOQUE_PUB), TO_CHAR(FMI.ITEM_ESTOQUE)) AS CODIGO,
                    COALESCE(PIE.DES_ITEM_ESTOQUE, 'Acessorio ' || TO_CHAR(FMI.ITEM_ESTOQUE)) AS NOME,
                    COALESCE(TO_CHAR(PIE.GRUPO), 'Acessorios') AS CATEGORIA,
                    SUM(CASE WHEN TT.TIPO = 'E' THEN -1 ELSE 1 END * COALESCE(FMI.QUANTIDADE, 0)) AS QUANTIDADE,
                    SUM(CASE WHEN TT.TIPO = 'E' THEN -1 ELSE 1 END * (
                        COALESCE(FMI.VAL_TOTAL_REAL_ITEM, 0)
                        - (COALESCE(FMI.VAL_DESCONTO, 0) - COALESCE(FMI.VAL_DESCONTO_FRANQUIA, 0))
                        + COALESCE(FMI.VAL_FRETE, 0)
                    )) AS FATURAMENTO,
                    SUM(CASE WHEN TT.TIPO = 'E' THEN -1 ELSE 1 END * (
                        COALESCE(FMI.VAL_TOTAL_REAL_ITEM, 0)
                        - (COALESCE(FMI.VAL_DESCONTO, 0) - COALESCE(FMI.VAL_DESCONTO_FRANQUIA, 0))
                        + COALESCE(FMI.VAL_FRETE, 0)
                        - COALESCE(FMI.VAL_CUSTO_MEDIO, 0)
                    )) AS RENTABILIDADE
                FROM FAT_MOVIMENTO_CAPA FMC
                INNER JOIN FAT_MOVIMENTO_ITEM FMI
                   ON FMI.EMPRESA = FMC.EMPRESA
                  AND FMI.REVENDA = FMC.REVENDA
                  AND FMI.NUMERO_NOTA_FISCAL = FMC.NUMERO_NOTA_FISCAL
                  AND FMI.SERIE_NOTA_FISCAL = FMC.SERIE_NOTA_FISCAL
                  AND FMI.TIPO_TRANSACAO = FMC.TIPO_TRANSACAO
                  AND FMI.CONTADOR = FMC.CONTADOR
                INNER JOIN FAT_TIPO_TRANSACAO TT
                   ON TT.TIPO_TRANSACAO = FMC.TIPO_TRANSACAO
                LEFT JOIN PEC_ITEM_ESTOQUE PIE
                   ON PIE.EMPRESA = FMI.EMPRESA
                  AND PIE.ITEM_ESTOQUE = FMI.ITEM_ESTOQUE
                WHERE TO_CHAR(FMC.DEPARTAMENTO) = '7'
                  AND COALESCE(FMC.NFE_SITUACAO, ' ') <> 'D'
                  AND FMC.DTA_ENTRADA_SAIDA BETWEEN :DATA_INICIO AND :DATA_FIM
                  AND (:EMPRESA IS NULL OR INSTR(',' || :EMPRESA || ',', ',' || TO_CHAR(FMC.EMPRESA) || ',') > 0)
                  AND (:REVENDA IS NULL OR INSTR(',' || :REVENDA || ',', ',' || TO_CHAR(FMC.REVENDA) || ',') > 0)
                GROUP BY FMI.ITEM_ESTOQUE, PIE.ITEM_ESTOQUE_PUB, PIE.DES_ITEM_ESTOQUE, PIE.GRUPO
                ORDER BY FATURAMENTO DESC
            )
            WHERE ROWNUM <= 10";

        public VeiculosBiService(IConfiguration configuration, AppDbContext context)
        {
            _context = context;
            _connectionString = GetOracleConnectionString(configuration);
        }

        public async Task<List<VeiculoAcessorioRankingDto>> LoadAcessoriosAsync(string role, VeiculosBiFilterDto filter)
        {
            await EnsureCanAccessAsync(role);
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Connection string Oracle nao configurada para o B.I de veiculos.");

            var dataInicio = (filter.DataInicio ?? DateTime.Today.AddMonths(-1)).Date;
            var dataFim = (filter.DataFim ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            if (dataInicio > dataFim)
                throw new InvalidOperationException("Data inicial nao pode ser maior que a data final.");

            var empresa = await ApplyEmpresaScopeAsync(role, NormalizeFilter(filter.Empresa));
            var revenda = NormalizeFilter(filter.Revenda);

            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = AcessoriosSql;
            command.CommandType = CommandType.Text;
            command.Parameters.Add("DATA_INICIO", OracleDbType.Date, dataInicio, ParameterDirection.Input);
            command.Parameters.Add("DATA_FIM", OracleDbType.Date, dataFim, ParameterDirection.Input);
            command.Parameters.Add("EMPRESA", OracleDbType.Varchar2, empresa, ParameterDirection.Input);
            command.Parameters.Add("REVENDA", OracleDbType.Varchar2, revenda, ParameterDirection.Input);

            var items = new List<VeiculoAcessorioRankingDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var faturamento = GetDecimal(reader, "FATURAMENTO");
                var rentabilidade = GetDecimal(reader, "RENTABILIDADE");
                items.Add(new VeiculoAcessorioRankingDto
                {
                    Codigo = GetString(reader, "CODIGO"),
                    Nome = GetString(reader, "NOME"),
                    Categoria = GetString(reader, "CATEGORIA"),
                    Quantidade = GetInt(reader, "QUANTIDADE"),
                    Faturamento = faturamento,
                    Rentabilidade = rentabilidade,
                    MargemPercentual = faturamento == 0 ? 0 : rentabilidade / faturamento * 100
                });
            }

            return items;
        }

        private async Task EnsureCanAccessAsync(string role)
        {
            if (RoleScope.IsAdmin(role) || RoleScope.IsTI(role))
                return;

            var perfil = PerfisService.NormalizePerfilName(role);
            var hasAccess = await _context.PerfilSistema
                .Where(p => p.Nome == perfil)
                .SelectMany(p => p.Acessos.Select(a => a.Chave))
                .AnyAsync(acesso => acesso == "veiculos-bi");
            if (!hasAccess)
                throw new UnauthorizedAccessException("Perfil sem acesso ao B.I de veiculos.");
        }

        private async Task<object> ApplyEmpresaScopeAsync(string role, object requestedEmpresa)
        {
            if (RoleScope.IsQualidadeNissan(role))
                return "2";

            var perfil = PerfisService.NormalizePerfilName(role);
            var empresasPermitidas = await _context.PerfilSistema
                .Where(p => p.Nome == perfil)
                .SelectMany(p => p.Empresas.Select(e => e.EmpresaNumero))
                .Distinct()
                .ToListAsync();
            if (empresasPermitidas.Count == 0)
                return requestedEmpresa;

            if (requestedEmpresa == DBNull.Value)
                return string.Join(",", empresasPermitidas);

            var requested = requestedEmpresa.ToString()?.Trim() ?? string.Empty;
            var requestedNumbers = requested
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var numero) ? numero : 0)
                .Where(numero => numero > 0)
                .Distinct()
                .ToList();

            if (requestedNumbers.Any(numero => !empresasPermitidas.Contains(numero)))
                throw new UnauthorizedAccessException("Perfil sem acesso a empresa selecionada no B.I de veiculos.");

            return string.Join(",", requestedNumbers);
        }

        private static object NormalizeFilter(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) || normalized.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                ? DBNull.Value
                : normalized;
        }

        private static string GetOracleConnectionString(IConfiguration configuration)
        {
            var environment = configuration["Oracle:Environment"]?.Trim();
            var key = environment?.StartsWith("Prod", StringComparison.OrdinalIgnoreCase) == true
                ? "OracleConnectionProduction"
                : "OracleConnectionDve";

            var selectedConnection = configuration.GetConnectionString(key);
            if (!string.IsNullOrWhiteSpace(selectedConnection))
                return selectedConnection;

            var fallbackConnection = configuration.GetConnectionString("OracleConnection");
            return string.IsNullOrWhiteSpace(fallbackConnection) ? string.Empty : fallbackConnection;
        }

        private static string GetString(DbDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
        }

        private static decimal GetDecimal(DbDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
                return 0;

            if (reader is OracleDataReader oracleReader)
            {
                var value = oracleReader.GetOracleDecimal(ordinal);
                if (value.IsNull)
                    return 0;

                return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
            }

            return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int GetInt(DbDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }
    }
}
