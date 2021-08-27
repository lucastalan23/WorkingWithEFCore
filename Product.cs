using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Packt.Shared
{
  public class Product
  {
    public int ProductID { get; set; }

    [Required]
    [StringLength(40)]
    public string ProductName { get; set; }

    [Column("UnitPrice", TypeName = "money")]
    public decimal? Cost { get; set; }

    [Column("UnitsInStock")]
    public short? Stock { get; set; }

    public bool Discontinued { get; set; }

<<<<<<< HEAD
    // define a relaçao da chave primária
    // para a tabela de categorias
=======
    // these two define the foreign key relationship
    // to the Categories table.
>>>>>>> 7323235c1afce145587432f4c48f5f8e805149a2
    public int CategoryID { get; set; }

    public virtual Category Category { get; set; }
  }
}
