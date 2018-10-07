using MoreLinq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Desafio5
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("File path is missing", nameof(args));

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var parser = new Parser();
            var sbResult = new StringBuilder();

            parser.LoadAndProcessJson(args[0]);

            RunQuestao1(parser, sbResult);
            RunQuestao2(parser, sbResult);
            RunQuestao3(parser, sbResult);
            RunQuestao4(parser, sbResult);

            Console.Write(sbResult.ToString());
        }

        #region Questoes

        private static void RunQuestao1(Parser parser, StringBuilder sbResult)
        {
            foreach (var areaStats in parser.AreaStats.MaxBy(p => p.Value.MaxSalario))
            {
                foreach (var funcionario in areaStats.Value.Max)
                {
                    sbResult.AppendLine($"global_max|{Encoding.UTF8.GetString(funcionario.Nome)} {funcionario.Sobrenome}|{areaStats.Value.MaxSalario:0.00}");
                }
            }

            foreach (var areaStats in parser.AreaStats.MinBy(p => p.Value.MinSalario))
            {
                foreach (var funcionario in areaStats.Value.Min)
                {
                    sbResult.AppendLine($"global_min|{Encoding.UTF8.GetString(funcionario.Nome)} {funcionario.Sobrenome}|{areaStats.Value.MinSalario:0.00}");
                }
            }

            sbResult.AppendLine($"global_avg|{(parser.TotalSalarios / parser.AreaStats.Sum(a => a.Value.TotalFuncionarios)):0.00}");
        }

        private static void RunQuestao2(Parser parser, StringBuilder sbResult)
        {
            foreach (var areaStats in parser.AreaStats)
            {
                foreach (var funcionario in areaStats.Value.Max)
                {
                    sbResult.AppendLine($"area_max|{parser.Areas[areaStats.Key]}|{Encoding.UTF8.GetString(funcionario.Nome)} {funcionario.Sobrenome}|{areaStats.Value.MaxSalario:0.00}");
                }

                foreach (var funcionario in areaStats.Value.Min)
                {
                    sbResult.AppendLine($"area_min|{parser.Areas[areaStats.Key]}|{Encoding.UTF8.GetString(funcionario.Nome)} {funcionario.Sobrenome}|{areaStats.Value.MinSalario:0.00}");
                }

                sbResult.AppendLine($"area_avg|{parser.Areas[areaStats.Key]}|{areaStats.Value.Salario / areaStats.Value.TotalFuncionarios:0.00}");
            }
        }

        private static void RunQuestao3(Parser parser, StringBuilder sbResult)
        {
            foreach (var areaStats in parser.AreaStats.MaxBy(p => p.Value.TotalFuncionarios))
            {
                sbResult.AppendLine($"most_employees|{parser.Areas[areaStats.Key]}|{areaStats.Value.TotalFuncionarios}");
            }

            foreach (var areaStats in parser.AreaStats.MinBy(p => p.Value.TotalFuncionarios))
            {
                sbResult.AppendLine($"least_employees|{parser.Areas[areaStats.Key]}|{areaStats.Value.TotalFuncionarios}");
            }
        }

        private static void RunQuestao4(Parser parser, StringBuilder sbResult)
        {
            foreach (var sobrenomeStats in parser.SobrenomeStats.Where(p => p.Value.TotalFuncionarios > 1))
            {
                foreach (var nome in sobrenomeStats.Value.Nomes)
                {
                    var sobrenome = sobrenomeStats.Value.Sobrenome;
                    sbResult.AppendLine($"last_name_max|{sobrenome}|{Encoding.UTF8.GetString(nome)} {sobrenome}|{sobrenomeStats.Value.MaxSalario:0.00}");
                }
            }
        }

        #endregion Questoes
    }
}