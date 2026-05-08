namespace DRFlowHub.Api.Dtos.ChamadosTI
{
    public class ChamadoTICreateDto
    {
        public string? Titulo { get; set; }
        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
        public string? Solicitante { get; set; }
        public string? Unidade { get; set; }
        public string? Departamento { get; set; }
        public string? Prioridade { get; set; }
        public string? Status { get; set; }
        public string? Responsavel { get; set; }
        public string? AcessoRemotoUrl { get; set; }
        public string? Observacoes { get; set; }
        public int Userid { get; set; }
        public IFormFile? Anexo { get; set; }
        public IFormFile? Imagem { get; set; }
    }
}
