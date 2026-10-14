using HalalStocks.Models;
using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class CalculatorInstructionFormViewModel
{
    public CalculatorType Type { get; set; } = CalculatorType.CapitalPurification;

    [StringLength(20000)]
    public string? ContentHtml { get; set; }
}
