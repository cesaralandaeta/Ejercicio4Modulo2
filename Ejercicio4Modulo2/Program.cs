using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Ejercicio4Modulo2.Domain.Entities;
using Ejercicio4Modulo2.Repository;
using Microsoft.EntityFrameworkCore;

namespace VentasMensualesApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            string path = $"{AppDomain.CurrentDomain.BaseDirectory}\\data.txt";

            ProcesarVentas(path);
            ConsultarVentas();
        }

        public static List<VentasMensuales> LeerArchivo(string filePath)
        {
            var ventas = new List<VentasMensuales>();
            var lineas = File.ReadAllLines(filePath);

            foreach (var linea in lineas)
            {
                var venta = new VentasMensuales
                {
                    Fecha = DateTime.ParseExact(linea.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    CodVendedor = linea.Substring(10, 3).Trim(),
                    Venta = decimal.Parse(linea.Substring(13, 11).Trim()),
                    VentaEmpresaGrande = linea.Substring(24, 1) == "S"
                };
                ventas.Add(venta);
            }

            return ventas;
        }

        public static void ProcesarVentas(string filePath)
        {
            var ventas = LeerArchivo(filePath);

            using (var context = new Ejercicio4Modulo2Context())
            {
                var parametria = context.Parametria.FirstOrDefault();
              
                DateTime parametriaValue;

                // Convertir la fecha de informe de Parametria a DateTime
                if (!DateTime.TryParseExact(parametria.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parametriaValue))
                {
                    throw new InvalidOperationException("La fecha de informe en la tabla Parametria no tiene el formato correcto.");
                }

                foreach (var venta in ventas)
                {
                    var errores = new List<string>();

                    if (string.IsNullOrEmpty(venta.CodVendedor))
                    {
                        errores.Add("Código de vendedor faltante");
                    }

                
                    if (venta.Fecha != parametriaValue)
                    {
                        errores.Add("Fecha del informe incorrecta");
                    }

                    if (venta.VentaEmpresaGrande != true && venta.VentaEmpresaGrande != false)
                    {
                        errores.Add("Flag Venta a empresa grande incorrecto");
                    }

                    if (errores.Any())
                    {
                        var rechazo = new Rechazos
                        {
                            RegistroOriginal = $"{venta.Fecha:yyyy-MM-dd}{venta.CodVendedor}{venta.Venta:0.00}{(venta.VentaEmpresaGrande ? "S" : "N")}",
                            Error = string.Join(", ", errores)
                        };
                        context.Rechazos.Add(rechazo);
                    }
                    else
                    {
                        context.VentasMensuales.Add(venta);
                    }
                }

                context.SaveChanges();
            }
        }

        public static void ConsultarVentas()
        {
            using (var context = new Ejercicio4Modulo2Context())
            {
                // Listar vendedores que hayan superado los 100.000 en el mes
                var vendedoresSuperaron = context.VentasMensuales
                    .GroupBy(v => v.CodVendedor)
                    .Where(g => g.Sum(v => v.Venta) > 100000)
                    .Select(g => new { CodigoVendedor = g.Key, TotalVenta = g.Sum(v => v.Venta) })
                    .ToList();

                foreach (var vendedor in vendedoresSuperaron)
                {
                    Console.WriteLine($"El vendedor {vendedor.CodigoVendedor} vendió {vendedor.TotalVenta}");
                }

                // Listar vendedores que NO hayan superado los 100.000 en el mes
                var vendedoresNoSuperaron = context.VentasMensuales
                    .GroupBy(v => v.CodVendedor)
                    .Where(g => g.Sum(v => v.Venta) <= 100000)
                    .Select(g => new { CodigoVendedor = g.Key, TotalVenta = g.Sum(v => v.Venta) })
                    .ToList();

                foreach (var vendedor in vendedoresNoSuperaron)
                {
                    Console.WriteLine($"El vendedor {vendedor.CodigoVendedor} vendió {vendedor.TotalVenta}");
                }

                // Listar vendedores que hayan vendido al menos una vez a una empresa grande
                var vendedoresEmpresaGrande = context.VentasMensuales
                    .Where(v => v.VentaEmpresaGrande)
                    .Select(v => v.CodVendedor)
                    .Distinct()
                    .ToList();

                foreach (var vendedor in vendedoresEmpresaGrande)
                {
                    Console.WriteLine($"El vendedor {vendedor}");
                }

                // Listar rechazos
                var rechazos = context.Rechazos.ToList();
                foreach (var rechazo in rechazos)
                {
                    Console.WriteLine($"Rechazo: {rechazo.RegistroOriginal} - Error: {rechazo.Error}");
                }
            }
        }
    }
}
