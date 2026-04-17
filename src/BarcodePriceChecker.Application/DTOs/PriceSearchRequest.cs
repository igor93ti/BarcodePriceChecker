using System.ComponentModel.DataAnnotations;

namespace BarcodePriceChecker.Application.DTOs;

public class PriceSearchRequest
{
    [Required(ErrorMessage = "Informe o código de barras do produto.")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Código de barras inválido.")]
    public string Barcode { get; set; } = string.Empty;

    [Range(0.01, 99999.99, ErrorMessage = "Informe um preço válido.")]
    public decimal? UserPrice { get; set; }
}
