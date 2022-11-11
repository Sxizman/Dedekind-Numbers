public static class DedekindNumberCalculator
{
    private static int threads;

    private static int n;
    private static int pointsCount;

    private static int permutationsCount;
    private static int[][] permutationsTable = null!;

    private static int DSize;
    private static ulong[] D = null!;

    private static int RSize;
    private static int[][] R = null!;

    private static ulong[] duals = null!;
    private static int[] etas = null!;

    private static string dValueString = null!;

    private static int BinarySearchInD(ulong f, int maxIndex)
    {
        var left = 0;
        var right = maxIndex;

        while (right - left > 1)
        {
            var mid = (left + right) >> 1;
            if (D[mid] > f)
                right = mid;
            else
                left = mid;
        }

        return left;
    }

    private static int BinarySearchInD(ulong f)
    {
        return BinarySearchInD(f, DSize);
    }

    private static int Factorial(int x)
    {
        var value = 1;
        for (var i = 2; i <= x; ++i)
        {
            value *= i;
        }

        return value;
    }

    private static void MultiplyNumberBy2(List<int> digits)
    {
        var carry = 0;
        for (var i = 0; i < digits.Count; ++i)
        {
            carry += digits[i] * 2;
            digits[i] = carry % 10;
            carry /= 10;
        }

        if (carry != 0)
            digits.Add(carry);
    }

    private static void IncrementNumber(List<int> digits)
    {
        var carry = 1;
        for (var i = 0; i < digits.Count; ++i)
        {
            carry += digits[i];
            digits[i] = carry % 10;
            carry /= 10;
        }

        if (carry != 0)
            digits.Add(carry);
    }

    private static void CombineDPartialNumbers(int[] highs, ulong[] lows, out int high, out ulong low)
    {
        high = 0;
        low = 0;
        for (var i = 0; i < threads; ++i)
        {
            low += lows[i];
            if (low < lows[i])
                ++high;
            high += highs[i];
        }
    }

    private static void MakeDNumberString(int high, ulong low)
    {
        var digits = new List<int> { 0 };
        for (var i = 30; i >= 0; --i)
        {
            MultiplyNumberBy2(digits);
            if (((high >> i) & 1) == 1)
                IncrementNumber(digits);
        }
        for (var i = 63; i >= 0; --i)
        {
            MultiplyNumberBy2(digits);
            if (((low >> i) & 1) == 1)
                IncrementNumber(digits);
        }

        dValueString = "";
        for (var i = digits.Count - 1; i >= 0; --i)
        {
            dValueString += (char)('0' + digits[i]);
        }
    }

    private static int[] GetVariablesPermutation(int permutationIndex)
    {
        var variablesPermutation = new int[n];
        for (var i = 1; i <= n; ++i)
        {
            variablesPermutation[n - i] = permutationIndex % i;

            for (var j = n - i + 1; j < n; ++j)
            {
                if (variablesPermutation[j] >= variablesPermutation[n - i])
                    ++variablesPermutation[j];
            }

            permutationIndex /= i;
        }

        return variablesPermutation;
    }

    private static int[] GetPointsPermutation(int[] variablesPermutation)
    {
        var pointsPermutation = new int[pointsCount];
        for (var i = 0; i < pointsCount; ++i)
        {
            var t = 0;
            for (var j = 0; j < n; ++j)
            {
                t |= ((i >> variablesPermutation[j]) & 1) << j;
            }

            pointsPermutation[i] = t;
        }

        return pointsPermutation;
    }

    private static void PreparePermutationsTable()
    {
        Console.Write("  Подготовка таблицы перестановок...");
        var beginTime = DateTime.Now;

        permutationsCount = Factorial(n);
        permutationsTable = new int[permutationsCount][];

        for (var i = 0; i < permutationsCount; ++i)
        {
            var bitsPermutation = GetVariablesPermutation(i);
            permutationsTable[i] = GetPointsPermutation(bitsPermutation);
        }

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  Таблица перестановок создана за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static ulong GetFunctionPermutation(ulong f, int permutationIndex)
    {
        var pointsPermutation = permutationsTable[permutationIndex];

        var fPermuted = 0ul;
        for (var i = 0; i < pointsCount; ++i)
        {
            fPermuted |= ((f >> pointsPermutation[i]) & 1) << i;
        }

        return fPermuted;
    }

    private static void MakeD()
    {
        Console.Write($"  Вычисление D{n}...");
        var beginTime = DateTime.Now;

        var D = new List<ulong> { 0, 1 };
        var previousCount = 2;

        for (var i = 0; i < n; ++i)
        {
            var shift = 1 << i;

            for (var j1 = 1; j1 < previousCount; ++j1)
            {
                for (var j2 = j1; j2 < previousCount; ++j2)
                {
                    var f1 = D[j1];
                    var f2 = D[j2];

                    if ((f1 & f2) == f1)
                        D.Add((f1 << shift) | f2);
                }
            }

            previousCount = D.Count;
        }

        DSize = D.Count;
        DedekindNumberCalculator.D = D.ToArray();

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  D{n} вычислено за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static void MakeR()
    {
        Console.Write($"  Вычисление R{n}...");
        var beginTime = DateTime.Now;

        var R = new List<int[]>();
        var usedD = new bool[DSize];

        for (var i = 0; i < DSize; ++i)
        {
            if (usedD[i])
                continue;

            var equivalent = new int[permutationsCount + 1];
            var equivalentCount = 0;
            for (var j = 0; j < permutationsCount; ++j)
            {
                var fPermuted = GetFunctionPermutation(D[i], j);
                var fPermutedIndexInD = BinarySearchInD(fPermuted);
                if (Array.IndexOf(equivalent, fPermutedIndexInD, 0, equivalentCount) != -1)
                    continue;

                equivalent[equivalentCount] = fPermutedIndexInD;
                ++equivalentCount;

                usedD[fPermutedIndexInD] = true;
            }

            equivalent[permutationsCount] = equivalentCount;
            R.Add(equivalent);
        }

        RSize = R.Count;
        DedekindNumberCalculator.R = R.ToArray();

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  R{n} вычислено за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static ulong GetDual(ulong f)
    {
        var fDual = 0ul;
        for (var i = 0; i < pointsCount; ++i)
        {
            fDual |= (~(f >> (pointsCount - i - 1)) & 1) << i;
        }

        return fDual;
    }

    private static void MakeDuals()
    {
        Console.Write($"  Подготовка таблицы двойственных функций для D{n}...");
        var beginTime = DateTime.Now;

        duals = new ulong[DSize];

        for (var i = 0; i < DSize; ++i)
        {
            duals[i] = GetDual(D[i]);
        }

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  Таблица двойственных функций для D{n} создана за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static int GetEta(int fIndexInD)
    {
        var f1 = D[fIndexInD];

        var eta = 1;
        for (var i = 0; i < fIndexInD; ++i)
        {
            var f2 = D[i];
            if ((f2 & f1) == f2)
                ++eta;
        }

        return eta;
    }

    private static void MakeEtasPartial(int threadIndex)
    {
        for (var i = threadIndex; i < RSize; i += threads)
        {
            var equivalent = R[i];
            var eqivalentEta = GetEta(equivalent[0]);

            var equivalentCount = equivalent[permutationsCount];
            for (var j = 0; j < equivalentCount; ++j)
            {
                etas[equivalent[j]] = eqivalentEta;
            }
        }
    }

    private static void MakeEtas()
    {
        Console.Write($"  Вычисление количества входящих функций для D{n}...");
        var beginTime = DateTime.Now;

        etas = new int[DSize];

        var tasks = new Task[threads];
        for (var i = 0; i < threads; ++i)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() => MakeEtasPartial(threadIndex));
        }
        Task.WaitAll(tasks);

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  Входящие функции для D{n} вычислены за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static void MakeDNumberPartial(int threadIndex, out int _high, out ulong _low)
    {
        var high = 0;
        var low = 0ul;

        for (var i = threadIndex; i < RSize; i += threads)
        {
            var f1 = D[R[i][0]];
            var f1Dual = duals[R[i][0]];

            var searchMax1 = BinarySearchInD(f1) + 1;
            var searchMax2 = BinarySearchInD(f1Dual) + 1;

            var equivalentCount = (long)R[i][permutationsCount];
            for (var j = 0; j < DSize; ++j)
            {
                var f2 = D[j];
                var f2Dual = duals[j];

                var s00 = f1 & f2;
                var s11 = f1Dual & f2Dual;
                var s00IndexInD = BinarySearchInD(s00, searchMax1);
                var s11IndexInD = BinarySearchInD(s11, searchMax2);

                var increment = (ulong)(equivalentCount * etas[s00IndexInD] * etas[s11IndexInD]);
                low += increment;
                if (low < increment)
                    ++high;
            }

            Console.WriteLine($"  поток {threadIndex}\tитерация {i} / {RSize}");
        }

        _high = high;
        _low = low;
    }

    private static void MakeDNumber()
    {
        Console.WriteLine($"  Вычисление значения d{n + 2}...");
        var beginTime = DateTime.Now;

        var highs = new int[threads];
        var lows = new ulong[threads];

        var tasks = new Task[threads];
        for (var i = 0; i < threads; ++i)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() => MakeDNumberPartial(threadIndex, out highs[threadIndex], out lows[threadIndex]));
        }
        Task.WaitAll(tasks);

        CombineDPartialNumbers(highs, lows, out var high, out var low);
        MakeDNumberString(high, low);

        var endTime = DateTime.Now;
        Console.WriteLine($"\r  Значение d{n + 2} вычислено за {(endTime - beginTime).TotalSeconds} секунд");
    }

    private static void FreeResources()
    {
        permutationsTable = null!;
        D = null!;
        R = null!;
        duals = null!;
        etas = null!;
        dValueString = null!;

        GC.Collect();
    }

    private static string CalucateDNumber(int _n, int _threads)
    {
        var beginTime = DateTime.Now;
        Console.WriteLine($"  НАЧАЛО ВЫЧИСЛЕНИЙ: {beginTime}");
        Console.WriteLine();

        threads = _threads;

        n = (_n > 6) ? (_n - 2) : _n;
        pointsCount = 1 << n;

        MakeD();
        if (_n > 6)
        {
            PreparePermutationsTable();
            MakeR();
            MakeDuals();
            MakeEtas();
            MakeDNumber();
        }

        var d = (_n > 6) ? dValueString : DSize.ToString();

        var endTime = DateTime.Now;
        Console.WriteLine();
        Console.WriteLine($"  КОНЕЦ ВЫЧИСЛЕНИЙ: {endTime}");
        Console.WriteLine($"  Общее время: {(endTime - beginTime).TotalSeconds} секунд");
        Console.WriteLine();

        FreeResources();

        return d;
    }

    private static int ReadInt(string message, int min, int max)
    {
        int value;
        while (true)
        {
            Console.Write(message);
            var isParsed = int.TryParse(Console.ReadLine(), out value);

            if (!isParsed)
                Console.WriteLine("Не число");
            else if (value < min || value > max)
                Console.WriteLine($"Допустимо значение от {min} до {max}");
            else
                break;
        }

        return value;
    }

    private static string ProcessNumberForPrint(string number)
    {
        var processed = "";
        for (var i = 0; i < number.Length; ++i)
        {
            if ((number.Length - i) % 3 == 0 && i > 0)
                processed += '.';

            processed += number[i];
        }

        return processed;
    }

    public static void Run()
    {
        Console.WriteLine(@"=================== Многопоточное вычисление количества монотонных функций для n переменных (n <= 8) ===================");
        Console.WriteLine();
        Console.WriteLine("  Автор:  Бикбов Герман");
        Console.WriteLine("  Github: https://github.com/Sxizman");
        Console.WriteLine();

        var n = ReadInt("  n = ", 0, 8);
        var threads = ReadInt("  Количество потоков: ", 1, 32768);
        Console.WriteLine();

        var d = CalucateDNumber(n, threads);
        Console.WriteLine($"  d{n} = {ProcessNumberForPrint(d)}");
        Console.WriteLine();

        Console.WriteLine("Нажмите Enter, чтобы выйти...");
        Console.ReadLine();
    }
}