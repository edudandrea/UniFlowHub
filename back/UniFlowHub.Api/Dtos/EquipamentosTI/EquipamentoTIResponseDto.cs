namespace UniFlowHub.Api.Dtos.EquipamentosTI
{
    public class EquipamentoTIResponseDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Patrimonio { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Responsavel { get; set; } = string.Empty;
        public DateTime DataMovimentacao { get; set; }
        public DateTime? DataPrevistaRetorno { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public string DocumentoUrl { get; set; } = string.Empty;
        public int Userid { get; set; }
    }
}
