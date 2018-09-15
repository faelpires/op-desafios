using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Collections;

namespace Desafio5
{
    internal class Program
    {
        #region Fields

        private static double _totalSalarios;
        private static Funcionario _currentFuncionario;
        private static StringBuilder _stringBuilderHashCode = new StringBuilder(27);
        private static StringBuilder _stringBuilderResult = new StringBuilder();
        private static readonly Dictionary<int, AreaStats> AreaStatsDict = new Dictionary<int, AreaStats>();
        private static readonly Dictionary<string, SobrenomeStats> SobrenomeStatsDict = new Dictionary<string, SobrenomeStats>();
        private static readonly Dictionary<int, byte[]> AreasDict = new Dictionary<int, byte[]>();

        #endregion Fields

        private static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("File path is missing", "args");

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            LoadAndProcessJson(args[0]);

                RunQuestao1();
                RunQuestao2();
                RunQuestao3();
                RunQuestao4();

            Console.Write(_stringBuilderResult.ToString());
        }

        #region Questoes

        private static void RunQuestao1()
        {
            foreach (var areaStats in AreaStatsDict.MaxBy(p => p.Value.MaxSalario))
            {
                foreach (var funcionario in areaStats.Value.Max)
                {
                    _stringBuilderResult.AppendLine($"global_max|{GetUTF8String(funcionario.Nome)} {GetUTF8String(funcionario.Sobrenome)}|{areaStats.Value.MaxSalario:0.00}");
                }
            }

            foreach (var areaStats in AreaStatsDict.MinBy(p => p.Value.MinSalario))
            {
                foreach (var funcionario in areaStats.Value.Min)
                {
                    _stringBuilderResult.AppendLine($"global_min|{GetUTF8String(funcionario.Nome)} {GetUTF8String(funcionario.Sobrenome)}|{areaStats.Value.MinSalario:0.00}");
                }
            }

            _stringBuilderResult.AppendLine($"global_avg|{(_totalSalarios / AreaStatsDict.Sum(a => a.Value.TotalFuncionarios)):0.00}");
        }

        private static void RunQuestao2()
        {
            foreach (var areaStats in AreaStatsDict)
            {
                foreach (var funcionario in areaStats.Value.Max)
                {
                    _stringBuilderResult.AppendLine($"area_max|{GetUTF8String(AreasDict[areaStats.Key])}|{GetUTF8String(funcionario.Nome)} {GetUTF8String(funcionario.Sobrenome)}|{areaStats.Value.MaxSalario:0.00}");
                }

                foreach (var funcionario in areaStats.Value.Min)
                {
                    _stringBuilderResult.AppendLine($"area_min|{GetUTF8String(AreasDict[areaStats.Key])}|{GetUTF8String(funcionario.Nome)} {GetUTF8String(funcionario.Sobrenome)}|{areaStats.Value.MinSalario:0.00}");
                }

                _stringBuilderResult.AppendLine($"area_avg|{GetUTF8String(AreasDict[areaStats.Key])}|{areaStats.Value.Salario / areaStats.Value.TotalFuncionarios:0.00}");
            }
        }

        private static void RunQuestao3()
        {
            foreach (var areaStats in AreaStatsDict.MaxBy(p => p.Value.TotalFuncionarios))
            {
                _stringBuilderResult.AppendLine($"most_employees|{GetUTF8String(AreasDict[areaStats.Key])}|{areaStats.Value.TotalFuncionarios}");
            }

            foreach (var areaStats in AreaStatsDict.MinBy(p => p.Value.TotalFuncionarios))
            {
                _stringBuilderResult.AppendLine($"least_employees|{GetUTF8String(AreasDict[areaStats.Key])}|{areaStats.Value.TotalFuncionarios}");
            }
        }

        private static void RunQuestao4()
        {
            foreach (var sobrenomeStats in SobrenomeStatsDict.Where(p => p.Value.TotalFuncionarios > 1))
            {
                foreach (var nome in sobrenomeStats.Value.Nomes)
                {
                    var sobrenome = GetUTF8String(sobrenomeStats.Value.Sobrenome);
                    _stringBuilderResult.AppendLine($"last_name_max|{sobrenome}|{GetUTF8String(nome)} {sobrenome}|{sobrenomeStats.Value.MaxSalario:0.00}");
                }
            }
        }

        #endregion Questoes

        #region Utils

        public static unsafe String ToThinString(byte[] src)
        {
            int c;
            var ret = String.Empty;
            if ((c = Array.IndexOf(src, byte.MinValue)) > 0)
                fixed (char* dst = (ret = new String('\0', c)))
                    do
                        dst[--c] = (char)src[c];  // fill new String by in-situ mutation
                    while (c > 0);

            return ret;
        }
        
        private static string GetUTF8String(byte[] bytes) => Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);

        public static unsafe string GetUnicodeString(byte[] bytes)
        {
            fixed (byte* pB = bytes)
            {
                _stringBuilderHashCode.Clear();

                for (int i = 0; i < bytes.Length && pB[i] != 0; i++)
                {
                    _stringBuilderHashCode.Append((char)pB[i]);
                }

                return _stringBuilderHashCode.ToString();
            }
        }

        private unsafe static double DoubleParse(string value)
        {
            double result = 0;
            var len = value.Length;
            double sign = 1;

            fixed (char* pstr = value)
            {
                var end = (pstr + len);
                var pc = pstr;
                char c = *pc;

                while (true)
                {
                    c = *pc++;
                    
                    if (c < '0' || c > '9') break;
                    
                    result = (result * 10.0) + (c - '0');
                }
           
                double exp = 0.1;
                
                while (pc < end)
                {
                    c = *pc++;
                    result += (c - '0') * exp;
                    exp *= 0.1;
                }
            }
            return sign * result;
        }

        private static unsafe void MoveToToken(FileStream sr, byte[] buffer, ref int read, byte* pBuffer, ref int i, char value)
        {
            while (true)
            {
                if (i == read)
                {
                    i = 0;
                    read = sr.Read(buffer, 0, buffer.Length);
                }

                if (pBuffer[i] == value)
                    break;
                else
                    i++;
            }
        }

        #endregion Utils

        #region LoadAndProcessJson

        private unsafe static void LoadAndProcessJson(string filePath)
        {
            const int bufferSize = 256 * 1024;

            using (var sr = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
            {
                var buffer = new byte[bufferSize];
                var maxValueLength = 27;
                var maxPropLength = 12;

                var funcionariosCurrentProperty = 0;
                var funcionariosPropertyValue = new byte[maxValueLength];
                var funcionariosPropertyValueIndex = 0;

                var areasPropertyName = new byte[maxPropLength];
                var areasPropertyValue = new byte[maxValueLength];
                var areasPropertyNameIndex = 0;
                var areasPropertyValueIndex = 0;

                var value = new StringBuilder();
                var currentArea = new Area();

                const int propNameNome = 1701670766;
                const int propNameCodigo = 1768189795;

                int read;

                Operations operation = Operations.FuncionariosMoveToItems;

                fixed (byte* pBuffer = buffer)
                {
                    while ((read = sr.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (var i = 0; i < read; i++)
                        {
                            switch (operation)
                            {
                                case Operations.FuncionariosMoveToItems:
                                    {
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '{');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '"');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '"');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, ':');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '[');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '{');

                                        operation = Operations.FuncionariosItemMoveToPropStart;

                                        break;
                                    }
                                case Operations.FuncionariosItemMoveToPropStart:
                                    {
                                        while (true)
                                        {
                                            if (i == read)
                                            {
                                                i = 0;
                                                read = sr.Read(buffer, 0, buffer.Length);
                                            }

                                            if (pBuffer[i] == '"')
                                            {
                                                operation = Operations.FuncionariosItemReadPropName;
                                                break;
                                            }
                                            else if (pBuffer[i] == '}')
                                            {
                                                break;
                                            }
                                            else if (pBuffer[i] == ']')
                                            {
                                                operation = Operations.AreasMoveToItems;
                                                break;
                                            }
                                            else
                                                i++;
                                        }

                                        break;
                                    }
                                case Operations.FuncionariosItemReadPropName:
                                    {
                                        funcionariosCurrentProperty++;

                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, ':');
                                        operation = Operations.FuncionariosItemReadValue;

                                        break;
                                    }
                                case Operations.FuncionariosItemReadValue:
                                    {
                                        funcionariosPropertyValue = new byte[maxValueLength];
                                        funcionariosPropertyValueIndex = 0;

                                        if (pBuffer[i] == '"') // if string
                                        {
                                            i++;

                                            while (true)
                                            {
                                                if (i == read)
                                                {
                                                    i = 0;
                                                    read = sr.Read(buffer, 0, buffer.Length);
                                                }

                                                if (pBuffer[i] == '"')
                                                    break;
                                                else
                                                    funcionariosPropertyValue[funcionariosPropertyValueIndex++] = pBuffer[i++];
                                            }
                                        }
                                        else // if numeric
                                        {
                                            while (true)
                                            {
                                                if (i == read)
                                                {
                                                    i = 0;
                                                    read = sr.Read(buffer, 0, buffer.Length);
                                                }

                                                if (pBuffer[i] == ',')
                                                    break;
                                                else if (funcionariosCurrentProperty != 1)
                                                    value.Append((char)pBuffer[i++]);
                                                else
                                                    i++;
                                            }
                                        }

                                        switch (funcionariosCurrentProperty)
                                        {
                                            case 1:

                                                operation = Operations.FuncionariosItemMoveToPropStart;
                                                //value.Clear();

                                                break;

                                            case 2:

                                                _currentFuncionario.Nome = funcionariosPropertyValue;
                                                operation = Operations.FuncionariosItemMoveToPropStart;

                                                break;

                                            case 3:

                                                _currentFuncionario.Sobrenome = funcionariosPropertyValue;
                                                operation = Operations.FuncionariosItemMoveToPropStart;

                                                break;

                                            case 4:

                                                _currentFuncionario.Salario = DoubleParse(value.ToString());

                                                operation = Operations.FuncionariosItemMoveToPropStart;
                                                value.Clear();

                                                break;

                                            case 5:

                                                funcionariosCurrentProperty = 0;
                                                _currentFuncionario.Area = BitConverter.ToInt32(funcionariosPropertyValue, 0);
                                                operation = Operations.FuncionariosItemMoveToPropStart;

                                                ComputeSobrenomeStats();
                                                
                                                ComputeAreaStats();

                                                break;
                                        }

                                        if (pBuffer[i] == '}')
                                        {
                                            operation = Operations.FuncionariosItemMoveToPropStart;
                                        }
                                        else if (pBuffer[i] == ']')
                                        {
                                            operation = Operations.AreasMoveToItems;
                                        }

                                        break;
                                    }
                                case Operations.AreasMoveToItems:
                                    {
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '"');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '"');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, ':');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '[');
                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, '{');

                                        operation = Operations.AreasItemMoveToPropStart;

                                        break;
                                    }
                                case Operations.AreasItemMoveToPropStart:
                                    {
                                        while (true)
                                        {
                                            if (i == read)
                                            {
                                                i = 0;
                                                read = sr.Read(buffer, 0, buffer.Length);
                                            }

                                            if (pBuffer[i] == '"')
                                            {
                                                operation = Operations.AreasItemReadPropName;
                                                break;
                                            }
                                            else if (pBuffer[i] == '}')
                                            {
                                                operation = Operations.AreasItemMoveToPropStart;
                                                break;
                                            }
                                            else
                                                i++;
                                        }

                                        break;
                                    }
                                case Operations.AreasItemReadPropName:
                                    {
                                        areasPropertyName = new byte[maxPropLength];
                                        areasPropertyNameIndex = 0;

                                        while (true)
                                        {
                                            if (i == read)
                                            {
                                                i = 0;
                                                read = sr.Read(buffer, 0, buffer.Length);
                                            }

                                            if (pBuffer[i] != '"')
                                                areasPropertyName[areasPropertyNameIndex++] = pBuffer[i++];
                                            else
                                                break;
                                        }

                                        MoveToToken(sr, buffer, ref read, pBuffer, ref i, ':');
                                        operation = Operations.AreasItemReadValue;

                                        break;
                                    }
                                case Operations.AreasItemReadValue:
                                    {
                                        areasPropertyValue = new byte[maxValueLength];
                                        areasPropertyValueIndex = 0;

                                        i++;

                                        while (true)
                                        {
                                            if (i == read)
                                            {
                                                i = 0;
                                                read = sr.Read(buffer, 0, buffer.Length);
                                            }

                                            if (pBuffer[i] == '"')
                                            {
                                                break;
                                            }
                                            else
                                                areasPropertyValue[areasPropertyValueIndex++] = pBuffer[i++];
                                        }

                                        switch (BitConverter.ToInt32(areasPropertyName, 0))
                                        {
                                            case propNameCodigo:
                                                currentArea.Codigo = BitConverter.ToInt32(areasPropertyValue, 0);
                                                operation = Operations.AreasItemMoveToPropStart;

                                                break;

                                            case propNameNome:
                                                currentArea.Nome = areasPropertyValue;
                                                operation = Operations.AreasItemMoveToPropStart;

                                                AreasDict.Add(currentArea.Codigo, currentArea.Nome);

                                                break;
                                        }

                                        if (pBuffer[i] == '}')
                                        {
                                            operation = Operations.AreasItemMoveToPropStart;
                                        }
                                        else if (pBuffer[i] == ']')
                                        {
                                            operation = Operations.AreasMoveToItems;
                                        }

                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        private static void ComputeAreaStats()
        {
            if (!AreaStatsDict.TryGetValue(_currentFuncionario.Area, out var areaStats))
            {
                AreaStatsDict.Add(_currentFuncionario.Area, new AreaStats
                {
                    Code = _currentFuncionario.Area,
                    Min = new List<FullName> { new FullName { Nome = _currentFuncionario.Nome, Sobrenome = _currentFuncionario.Sobrenome } },
                    Max = new List<FullName> { new FullName { Nome = _currentFuncionario.Nome, Sobrenome = _currentFuncionario.Sobrenome } },
                    TotalFuncionarios = 1,
                    MaxSalario = _currentFuncionario.Salario,
                    MinSalario = _currentFuncionario.Salario,
                    Salario = _currentFuncionario.Salario
                });
            }
            else
            {
                areaStats.TotalFuncionarios++;
                areaStats.Salario += _currentFuncionario.Salario;

                if (_currentFuncionario.Salario >= areaStats.MaxSalario)
                {
                    if (_currentFuncionario.Salario > areaStats.MaxSalario)
                    {
                        areaStats.Max = new List<FullName>();
                        areaStats.MaxSalario = _currentFuncionario.Salario;
                    }

                    areaStats.Max.Add(new FullName { Nome = _currentFuncionario.Nome, Sobrenome = _currentFuncionario.Sobrenome });
                }

                if (_currentFuncionario.Salario <= areaStats.MinSalario)
                {
                    if (_currentFuncionario.Salario < areaStats.MinSalario)
                    {
                        areaStats.Min = new List<FullName>();
                        areaStats.MinSalario = _currentFuncionario.Salario;
                    }

                    areaStats.Min.Add(new FullName { Nome = _currentFuncionario.Nome, Sobrenome = _currentFuncionario.Sobrenome });
                }
            }

            _totalSalarios += _currentFuncionario.Salario;
        }

        private static void ComputeSobrenomeStats()
        {
            var sobrenome = GetUnicodeString(_currentFuncionario.Sobrenome);

            if (!SobrenomeStatsDict.TryGetValue(sobrenome, out var sobrenomeStats))
            {
                SobrenomeStatsDict.Add(sobrenome, new SobrenomeStats
                {
                    Sobrenome = _currentFuncionario.Sobrenome,
                    TotalFuncionarios = 1,
                    MaxSalario = _currentFuncionario.Salario,
                    Nomes = new List<byte[]> { _currentFuncionario.Nome }
                });
            }
            else
            {
                sobrenomeStats.TotalFuncionarios++;

                if (!(_currentFuncionario.Salario >= sobrenomeStats.MaxSalario)) return;

                if (_currentFuncionario.Salario > sobrenomeStats.MaxSalario)
                {
                    sobrenomeStats.Nomes = new List<byte[]>();
                    sobrenomeStats.MaxSalario = _currentFuncionario.Salario;
                }

                sobrenomeStats.Nomes.Add(_currentFuncionario.Nome);
            }
        }

        #endregion LoadAndProcessJson
    }
}
