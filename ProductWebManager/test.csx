#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.0"
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Data;

var options = new DbContextOptionsBuilder<ProductManagerContext>()
    .UseSqlServer("workstation id=ProductWebManager.mssql.somee.com;packet size=4096;user id=fractum_SQLLogin_2;pwd=2bhzastl5d;data source=ProductWebManager.mssql.somee.com;persist security info=False;initial catalog=ProductWebManager;TrustServerCertificate=True")
    .Options;
using var db = new ProductManagerContext(options);
var p = db.Products.FirstOrDefault(p => p.Name.Contains("пастила"));
if(p!=null) Console.WriteLine($"Name: {p.Name}, IsPieceBased: {p.IsPieceBased}, AvgWeight: {p.AverageWeightGrams}, Cals: {p.Calories}");
else Console.WriteLine("Not found");
