using HDKmall.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace HDKmall.Scratch
{
    public class CheckProduct
    {
        public static void Run(ApplicationDbContext context)
        {
            var product = context.Products
                .Include(p => p.Versions)
                .FirstOrDefault(p => p.Id == 1);
            
            if (product != null)
            {
                Console.WriteLine($"Product ID: {product.Id}");
                Console.WriteLine($"Name: {product.Name}");
                Console.WriteLine($"ProductType: {product.ProductType}");
                Console.WriteLine($"Versions Count: {product.Versions.Count}");
                foreach(var v in product.Versions) {
                    Console.WriteLine($" - Version: {v.Name}, Price: {v.BasePrice}");
                }
            }
            else
            {
                Console.WriteLine("Product with ID 1 not found.");
            }
        }
    }
}
