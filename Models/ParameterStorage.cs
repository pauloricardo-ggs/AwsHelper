using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AwsHelper.Models;

public class ParameterStorage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "A descrição deve ter entre 2 e 50 caracteres.")]
    [DisplayName("Descrição")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O caminho é obrigatório.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O caminho deve ter entre 2 e 200 caracteres.")]
    [DisplayName("Caminho")]
    public string Path { get; set; } = string.Empty;
}