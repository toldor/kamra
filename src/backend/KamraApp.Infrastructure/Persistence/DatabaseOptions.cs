using System.ComponentModel.DataAnnotations;

namespace KamraApp.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionStrings:Default is required.")]
    public string ConnectionString { get; set; } = "";
}
