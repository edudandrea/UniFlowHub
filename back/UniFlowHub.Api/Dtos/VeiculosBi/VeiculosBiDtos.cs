namespace UniFlowHub.Api.Dtos.VeiculosBi
{
    public class VeiculosBiFilterDto
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? Empresa { get; set; }
        public string? Revenda { get; set; }
    }

    public class VeiculoAcessorioRankingDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Faturamento { get; set; }
        public decimal MargemPercentual { get; set; }
        public decimal Rentabilidade { get; set; }
    }
}
