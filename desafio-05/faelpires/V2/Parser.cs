using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Desafio5
{
    public class Parser
    {
        #region Fields
        private double _totalSalarios;
        private readonly Dictionary<int, AreaStats> _areaStats = new Dictionary<int, AreaStats>();
        private readonly Dictionary<int, SobrenomeStats> _sobrenomeStats = new Dictionary<int, SobrenomeStats>();
        private readonly Dictionary<int, string> _areasDict = new Dictionary<int, string>();

        #endregion Fields

        #region Properties

        public double TotalSalarios => _totalSalarios;

        public Dictionary<int, AreaStats> AreaStats => _areaStats;

        public Dictionary<int, SobrenomeStats> SobrenomeStats => _sobrenomeStats;

        public Dictionary<int, string> Areas => _areasDict;

        #endregion Properties

        private static double DoubleParse(Span<byte> value)
        {
            double result = 0;

            var indexOf = value.IndexOf((byte)'.');

            for (var i = 0; i < indexOf; i++)
            {
                result = (result * 10.0) + (value[i] - '0');
            }

            var exp = 0.1;

            for (var i = indexOf + 1; i < value.Length; i++)
            {
                result += (value[i] - '0') * exp;
                exp *= 0.1;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public unsafe void LoadAndProcessJson(string filePath)
        {
            const int bufferSize = 256 * 1024;

            using (var sr = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            {                

                var buffer = new byte[bufferSize];
                fixed (byte* pBuffer = buffer)
                {
                    var span = new Span<byte>(pBuffer, bufferSize);
                    int start;
                    int end;
                    int lastPosition;
                    var isPartial = false;
                    var isFirst = true;
                    byte[] partialContent = null;
                    int read;

                    while ((read = sr.Read(span)) > 0)
                    {
                        if (isFirst)
                        {
                            lastPosition = 1;
                            isFirst = false;
                        }
                        else
                            lastPosition = 0;

                        while (lastPosition < read)
                        {
                            if (isPartial)
                            {
                                var subSpan = span.Slice(lastPosition);
                                end = subSpan.IndexOf((byte)'}') + 1;

                                lastPosition += end;

                                var elementSpan = subSpan.Slice(0, end);

                                ParseFuncionario(partialContent.Concat(elementSpan.ToArray()).ToArray());

                                isPartial = false;
                            }
                            else
                            {
                                var subSpan = span.Slice(lastPosition);

                                start = subSpan.IndexOf((byte)'{');
                                end = subSpan.IndexOf((byte)'}');

                                if (start == -1)
                                    break;

                                if (end == -1)
                                {
                                    isPartial = true;
                                    partialContent = subSpan.Slice(start).ToArray();

                                    break;
                                }

                                lastPosition += end + 1;

                                if (start < end)
                                    ParseFuncionario(subSpan.Slice(start, end - start + 1));
                            }
                        }
                    }
                }
            }
        }

        private void ParseFuncionario(Span<byte> value)
        {
            var funcionario = new Funcionario();

            var start = value.IndexOf((byte)'"') + 1;

            if (value[start] != 'i')
            {
                ParseArea(value);
                return;
            }


            start = value.IndexOf<byte>((byte)',', start);
            start = value.IndexOf<byte>((byte)':', start);
            start = value.IndexOf<byte>((byte)'"', start) + 1;
            funcionario.Nome = value.Slice(start, value.IndexOf<byte>((byte)'"', start + 1) - start).ToArray();

            start = value.IndexOf<byte>((byte)',', start);
            start = value.IndexOf<byte>((byte)':', start);
            start = value.IndexOf<byte>((byte)'"', start) + 1;
            funcionario.Sobrenome = Encoding.UTF8.GetString(value.Slice(start, value.IndexOf<byte>((byte)'"', start) - start));

            start = value.IndexOf<byte>((byte)':', start) + 1;
            funcionario.Salario = DoubleParse(value.Slice(start, value.IndexOf<byte>((byte)',', start + 1) - start));

            start = value.IndexOf<byte>((byte)':', start);
            start = value.IndexOf<byte>((byte)'"', start) + 1;
            funcionario.Area = Encoding.UTF8.GetString(value.Slice(start, value.IndexOf<byte>((byte)'"', start) - start)).GetHashCode();

            ComputeAreaStats(funcionario);
            ComputeSobrenomeStats(funcionario);
        }

        private void ParseArea(Span<byte> value)
        {
            var start = value.IndexOf((byte)'"') + 10;
            var subSpan = value.Slice(start);
            var codigo = Encoding.UTF8.GetString(subSpan.Slice(0, subSpan.IndexOf((byte)'"'))).GetHashCode();

            start = subSpan.IndexOf((byte)':');
            subSpan = subSpan.Slice(start);
            start = subSpan.IndexOf((byte)'"') + 1;
            subSpan = subSpan.Slice(start);
            var nome = Encoding.UTF8.GetString(subSpan.Slice(0, subSpan.IndexOf((byte)'"')));
            
            _areasDict.Add(codigo, nome);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeAreaStats(Funcionario funcionario)
        {
            if (!_areaStats.TryGetValue(funcionario.Area, out var areaStats))
            {
                _areaStats.Add(funcionario.Area, new AreaStats
                {
                    Codigo = funcionario.Area,
                    Min = new List<FullName> { new FullName { Nome = funcionario.Nome, Sobrenome = funcionario.Sobrenome } },
                    Max = new List<FullName> { new FullName { Nome = funcionario.Nome, Sobrenome = funcionario.Sobrenome } },
                    TotalFuncionarios = 1,
                    MaxSalario = funcionario.Salario,
                    MinSalario = funcionario.Salario,
                    Salario = funcionario.Salario
                });
            }
            else
            {
                areaStats.TotalFuncionarios++;
                areaStats.Salario += funcionario.Salario;

                if (funcionario.Salario >= areaStats.MaxSalario)
                {
                    if (funcionario.Salario > areaStats.MaxSalario)
                    {
                        areaStats.Max = new List<FullName>();
                        areaStats.MaxSalario = funcionario.Salario;
                    }

                    areaStats.Max.Add(new FullName { Nome = funcionario.Nome, Sobrenome = funcionario.Sobrenome });
                }

                if (funcionario.Salario <= areaStats.MinSalario)
                {
                    if (funcionario.Salario < areaStats.MinSalario)
                    {
                        areaStats.Min = new List<FullName>();
                        areaStats.MinSalario = funcionario.Salario;
                    }

                    areaStats.Min.Add(new FullName { Nome = funcionario.Nome, Sobrenome = funcionario.Sobrenome });
                }
            }

            _totalSalarios += funcionario.Salario;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeSobrenomeStats(Funcionario funcionario)
        {
            var hash = funcionario.Sobrenome.GetHashCode();

            if (!_sobrenomeStats.TryGetValue(hash, out var sobrenomeStats))
            {
                _sobrenomeStats.Add(hash, new SobrenomeStats
                {
                    Sobrenome = funcionario.Sobrenome,
                    TotalFuncionarios = 1,
                    MaxSalario = funcionario.Salario,
                    Nomes = new List<byte[]> { funcionario.Nome }
                });
            }
            else
            {
                sobrenomeStats.TotalFuncionarios++;

                if (!(funcionario.Salario >= sobrenomeStats.MaxSalario)) return;

                if (funcionario.Salario > sobrenomeStats.MaxSalario)
                {
                    sobrenomeStats.Nomes = new List<byte[]>();
                    sobrenomeStats.MaxSalario = funcionario.Salario;
                }

                sobrenomeStats.Nomes.Add(funcionario.Nome);
            }
        }
    }
}