using static System.Console;
using Packt.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using System.Xml;
using System.IO; 
using System.Text.Json; 
using Microsoft.EntityFrameworkCore;
using static System.IO.Path;
using static System.Environment;
namespace WorkingWithEFCore
{
  class Program
  {
    static void QueryingCategories()
    {
      using (var db = new Northwind())
      {
        var loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        WriteLine("Categories and how many products they have:");

        IQueryable<Category> cats;

        db.ChangeTracker.LazyLoadingEnabled = false;

        Write("Enable eager loading? (Y/N): ");
        bool eagerloading = (ReadKey().Key == ConsoleKey.Y);
        bool explicitloading = false;
        WriteLine();
        if (eagerloading)
        {
          cats = db.Categories.Include(c => c.Products);
        }
        else
        {
          cats = db.Categories;
          Write("Enable explicit loading? (Y/N): ");
          explicitloading = (ReadKey().Key == ConsoleKey.Y);
          WriteLine();
        }

        foreach (Category c in cats)
        {
          if (explicitloading)
          {
            Write($"Explicitly load products for {c.CategoryName}? (Y/N): ");
            ConsoleKeyInfo key = ReadKey();
            WriteLine();

            if (key.Key == ConsoleKey.Y)
            {
              var products = db.Entry(c).Collection(c2 => c2.Products);
              if (!products.IsLoaded) products.Load();
            }
          }
          WriteLine($"{c.CategoryName} has {c.Products.Count} products.");
        }
      }
    }

    static void FilteredIncludes()
    {
      using (var db = new Northwind())
      {
        Write("Enter a minimum for units in stock: ");
        string unitsInStock = ReadLine();
        int stock = int.Parse(unitsInStock);

        IQueryable<Category> cats = db.Categories
          .Include(c => c.Products.Where(p => p.Stock >= stock));

        WriteLine($"ToQueryString: {cats.ToQueryString()}");

        foreach (Category c in cats)
        {
          WriteLine($"{c.CategoryName} has {c.Products.Count} products with a minimum of {stock} units in stock.");

          foreach(Product p in c.Products)
          {
            WriteLine($"  {p.ProductName} has {p.Stock} units in stock.");
          }
        }
      }
    }

    static void QueryingProducts()
    {
      using (var db = new Northwind())
      {
        var loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        WriteLine("Products that cost more than a price, highest at top.");
        string input;
        decimal price;
        do
        {
          Write("Enter a product price: ");
          input = ReadLine();
        } while (!decimal.TryParse(input, out price));

        IQueryable<Product> prods = db.Products
          .Where(product => product.Cost > price)
          .OrderByDescending(product => product.Cost);

        foreach (Product item in prods)
        {
          WriteLine(
            "{0}: {1} costs {2:$#,##0.00} and has {3} in stock.",
            item.ProductID, item.ProductName, item.Cost, item.Stock);
        }
      }
    }

    static void QueryingWithLike()
    {
      using (var db = new Northwind())
      {
        var loggerFactory = db.GetService<ILoggerFactory>();
        loggerFactory.AddProvider(new ConsoleLoggerProvider());

        Write("Enter part of a product name: ");
        string input = ReadLine();

        IQueryable<Product> prods = db.Products
          .Where(p => EF.Functions.Like(p.ProductName, $"%{input}%"));

        foreach (Product item in prods)
        {
          WriteLine("{0} has {1} units in stock. Discontinued? {2}",
            item.ProductName, item.Stock, item.Discontinued);
        }
      }
    }

    static bool AddProduct(int categoryID, string productName, decimal? price)
    {
      using (var db = new Northwind())
      {
        var newProduct = new Product
        {
          CategoryID = categoryID,
          ProductName = productName,
          Cost = price
        };

        // mark product as added in change tracking
        db.Products.Add(newProduct);

        // save tracked changes to database 
        int affected = db.SaveChanges();
        return (affected == 1);
      }
    }

    static void ListProducts()
    {
      using (var db = new Northwind())
      {
        WriteLine("{0,-3} {1,-35} {2,8} {3,5} {4}",
          "ID", "Product Name", "Cost", "Stock", "Disc.");

        foreach (var item in db.Products.OrderByDescending(p => p.Cost))
        {
          WriteLine("{0:000} {1,-35} {2,8:$#,##0.00} {3,5} {4}",
            item.ProductID, item.ProductName, item.Cost,
            item.Stock, item.Discontinued);
        }
      }
    }

    static bool IncreaseProductPrice(string name, decimal amount)
    {
      using (var db = new Northwind())
      {
        // get first product whose name starts with name
        Product updateProduct = db.Products.First(
          p => p.ProductName.StartsWith(name));

        updateProduct.Cost += amount;

        int affected = db.SaveChanges();
        return (affected == 1);
      }
    }

    static int DeleteProducts(string name)
    {
      using (var db = new Northwind())
      {
        using (IDbContextTransaction t = db.Database.BeginTransaction())
        {
          WriteLine("Transaction isolation level: {0}",
            t.GetDbTransaction().IsolationLevel);

          var products = db.Products.Where(
            p => p.ProductName.StartsWith(name));

          db.Products.RemoveRange(products);

          int affected = db.SaveChanges();
          t.Commit();
          return affected;
        }
      }
    }

    static void Main(string[] args)
    {
      ListProducts();
      FilteredIncludes();
      QueryingCategories();
      QueryingProducts();
      //AddProduct(1000, "teste1", 10);
      //DeleteProducts();
      ListProducts();
      using (var db = new Northwind())
      {
        IQueryable<Category> cats = db.Categories.Include(c => c.Products);

        GenerateXmlFile(cats, useAttributes: false);
        GenerateCsvFile(cats);
        GenerateJsonFile(cats);
      }
    }
    private delegate void WriteDataDelegate(string name, string value);

    private static void GenerateXmlFile(IQueryable<Category> cats, bool useAttributes = true)
    {
      string which = useAttributes ? "attibutes" : "elements";

      string xmlFile = $"categories-and-products-using-{which}.xml";

      using (FileStream xmlStream = File.Create(
        Combine(CurrentDirectory, xmlFile)))
      {
        using (XmlWriter xml = XmlWriter.Create(xmlStream,
          new XmlWriterSettings { Indent = true }))
        {

          WriteDataDelegate writeMethod;

          if (useAttributes)
          {
            writeMethod = xml.WriteAttributeString;
          }
          else
          {
            writeMethod = xml.WriteElementString;
          }

          xml.WriteStartDocument();
          xml.WriteStartElement("categories");

          foreach (Category c in cats)
          {
            xml.WriteStartElement("category");
            writeMethod("id", c.CategoryID.ToString());
            writeMethod("name", c.CategoryName);
            writeMethod("desc", c.Description);
            writeMethod("product_count", c.Products.Count.ToString());
            xml.WriteStartElement("products");

            foreach (Product p in c.Products)
            {
              xml.WriteStartElement("product");

              writeMethod("id", p.ProductID.ToString());
              writeMethod("name", p.ProductName);
              writeMethod("cost", p.Cost.Value.ToString());
              writeMethod("stock", p.Stock.ToString());
              writeMethod("discontinued", p.Discontinued.ToString());

              xml.WriteEndElement(); // </product>
            }
            xml.WriteEndElement(); // </products>
            xml.WriteEndElement(); // </category>
          }
          xml.WriteEndElement(); // </categories>
        }
      }

      WriteLine("{0} contains {1:N0} bytes.",
        arg0: xmlFile,
        arg1: new FileInfo(xmlFile).Length);
    }
  private static void GenerateCsvFile(IQueryable<Category> cats)
    {
      string csvFile = "categories-and-products.csv";

      using (FileStream csvStream = File.Create(Combine(CurrentDirectory, csvFile)))
      {
        using (var csv = new StreamWriter(csvStream))
        {

          csv.WriteLine("CategoryID,CategoryName,Description,ProductID,ProductName,Cost,Stock,Discontinued");

          foreach (Category c in cats)
          {
            foreach (Product p in c.Products)
            {
              csv.Write("{0},\"{1}\",\"{2}\",",
                arg0: c.CategoryID.ToString(),
                arg1: c.CategoryName,
                arg2: c.Description);

              csv.Write("{0},\"{1}\",{2},",
                arg0: p.ProductID.ToString(),
                arg1: p.ProductName,
                arg2: p.Cost.Value.ToString());

              csv.WriteLine("{0},{1}",
                arg0: p.Stock.ToString(),
                arg1: p.Discontinued.ToString());
            }
          }
        }
      }

      WriteLine("{0} contains {1:N0} bytes.",
        arg0: csvFile,
        arg1: new FileInfo(csvFile).Length);
    }

    private static void GenerateJsonFile(IQueryable<Category> cats)
    {
      string jsonFile = "categories-and-products.json";

      using (FileStream jsonStream = File.Create(Combine(CurrentDirectory, jsonFile)))
      {
        using (var json = new Utf8JsonWriter(jsonStream,
          new JsonWriterOptions { Indented = true }))
        {
          json.WriteStartObject();
          json.WriteStartArray("categories");

          foreach (Category c in cats)
          {
            json.WriteStartObject();

            json.WriteNumber("id", c.CategoryID);
            json.WriteString("name", c.CategoryName);
            json.WriteString("desc", c.Description);
            json.WriteNumber("product_count", c.Products.Count);

            json.WriteStartArray("products");

            foreach (Product p in c.Products)
            {
              json.WriteStartObject();

              json.WriteNumber("id", p.ProductID);
              json.WriteString("name", p.ProductName);
              json.WriteNumber("cost", p.Cost.Value);
              json.WriteNumber("stock", p.Stock.Value);
              json.WriteBoolean("discontinued", p.Discontinued);

              json.WriteEndObject(); // product
            }
            json.WriteEndArray(); // products
            json.WriteEndObject(); // category
          }
          json.WriteEndArray(); // categories
          json.WriteEndObject();
        }
      }

      WriteLine("{0} contains {1:N0} bytes.",
        arg0: jsonFile,
        arg1: new FileInfo(jsonFile).Length);
    }
  }
}
