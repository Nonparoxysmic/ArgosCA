using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ArgosCA.Bot.Dice;

internal static class LegacyDiceRoller
{
    readonly static int diceLimit = 1_000_000;
    readonly static Regex diceCount = new(@"\d*(?=d)");
    readonly static Regex diceRoll = new(@"\d*d(\d+|%|F)([HL]\d*)?(?=([+\-*]|$))");
    readonly static Regex validExpression = new(@"^-?(\d+|\d*d(\d+|%|F)([HL]\d*)?)([+\-*]-?(\d+|\d*d(\d+|%|F)([HL]\d*)?))*$");
    readonly static string NL = Environment.NewLine;

    internal static string EvaluateUserInput(string input)
    {
        input = RemoveWhiteSpace(input);
        string expression = ProcessInput(input);
        string output = $"Input: `{expression}`{NL}";
        if (validExpression.IsMatch(expression))
        {
            return output + EvaluateExpression(expression);
        }
        else
        {
            return output + "Error: Invalid input.";
        }
    }

    private static string RemoveWhiteSpace(string input)
    {
        char[] charArray = input.ToCharArray();
        int j = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (!char.IsWhiteSpace(charArray[i]))
            {
                charArray[j++] = charArray[i];
            }
        }
        return new string(charArray, 0, j);
    }

    private static string ProcessInput(string input)
    {
        char[] charArray = input.ToCharArray();
        int j = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = charArray[i];
            switch (c)
            {
                case 'D':
                    charArray[j++] = 'd';
                    break;
                case 'f':
                case 'h':
                case 'l':
                    charArray[j++] = char.ToUpper(c);
                    break;
                case 'K':
                case 'k':
                    if (i == input.Length - 1)
                    {
                        charArray[j++] = 'H';
                        break;
                    }
                    char next = charArray[i + 1];
                    if (next == 'H' || next == 'h')
                    {
                        charArray[j++] = 'H';
                        i++;
                    }
                    else if (next == 'L' || next == 'l')
                    {
                        charArray[j++] = 'L';
                        i++;
                    }
                    else
                    {
                        charArray[j++] = 'H';
                    }
                    break;
                default:
                    charArray[j++] = c;
                    break;
            }
        }
        return new string(charArray, 0, j);
    }

    private static string EvaluateExpression(string expression)
    {
        string output = "";
        int diceRolled = 0;
        while (true)
        {
            Match rollExpression = diceRoll.Match(expression);
            if (!rollExpression.Success) { break; }

            string diceCountDigits = diceCount.Match(rollExpression.Value).Value;
            int diceToRoll;
            if (diceCountDigits.Length == 0) { diceToRoll = 1; }
            else if (!int.TryParse(diceCountDigits, out diceToRoll) || diceToRoll > diceLimit)
            {
                return $"Error: Cannot evaluate {NL}`{rollExpression.Value}`{NL}(Too many dice.)";
            }
            if (diceToRoll > diceLimit - diceRolled)
            {
                return $"Error: Cannot evaluate expression.{NL}(Too many dice.)";
            }
            diceRolled += diceToRoll;

            RollResult rollResult = EvaluateRoll(rollExpression.Value, diceToRoll);

            if (!rollResult.Success)
            {
                return $"Error: {rollResult.Error}";
            }

            output += $"`{rollResult.Expression}`: {rollResult}{NL}";

            expression = string.Concat(expression[0..rollExpression.Index],
                rollResult.Value.ToString(),
                expression[(rollExpression.Index + rollExpression.Value.Length)..]);
        }

        if (DoMath(expression, out long finalResult, out string remaining))
        {
            return output + $"Result: **{finalResult:N0}**";
        }
        else
        {
            return output + $"Error: Unable to parse and evaluate {NL}`{remaining}`";
        }
    }

    private static RollResult EvaluateRoll(string expression, int quantity)
    {
        string processedExpression = expression[(expression.IndexOf('d') + 1)..];
        processedExpression = processedExpression.Replace("%", "100");

        int keep = quantity;
        foreach (char c in new char[] { 'H', 'L' })
        {
            int index = processedExpression.IndexOf(c);
            if (index < 0) { continue; }
            string digits = processedExpression[(index + 1)..];
            if (digits.Length == 0) { keep = 1; }
            else if (!int.TryParse(digits, out keep))
            {
                keep = quantity;
            }
            if (c == 'L') { keep *= -1; }
            processedExpression = processedExpression[0..index];
        }

        if (processedExpression.Contains('F'))
        {
            RollResult rollResultF = RollDiceF(quantity, keep);
            rollResultF.Expression = expression;
            return rollResultF;
        }

        if (int.TryParse(processedExpression, out int faces))
        {
            RollResult rollResult = RollDice(quantity, faces, keep);
            rollResult.Expression = expression;
            return rollResult;
        }
        return new RollResult(expression, $"Cannot parse `{processedExpression}` number of faces.");
    }

    private static RollResult RollDice(int quantity, int faces, int keep)
    {
        if (faces == 0)
        {
            return new RollResult("Cannot roll a die with zero faces.");
        }
        if (quantity == 0)
        {
            return new RollResult(0);
        }
        if (faces == 1)
        {
            return new RollResult(Math.Min(quantity, Math.Abs(keep)));
        }

        int[] dieResults = new int[quantity];
        bool[] dieKeeps = new bool[quantity];

        // Roll the dice.
        for (int i = 0; i < quantity; i++)
        {
            dieResults[i] = RandomNumberGenerator.GetInt32(faces) + 1;
        }

        // If keeping no dice or all of them...
        if (keep == 0 || Math.Abs(keep) >= quantity)
        {
            if (keep != 0)
            {
                for (int i = 0; i < quantity; i++)
                {
                    dieKeeps[i] = true;
                }
            }
            return new RollResult(dieResults, dieKeeps);
        }

        // Create a copy of the die results and sort them from lowest to highest.
        int[] sortedResults = (int[])dieResults.Clone();
        Array.Sort(sortedResults);
        // If keeping the highest results...
        if (keep > 0)
        {
            // Reverse the sorted results to be highest to lowest.
            Array.Reverse(sortedResults);
        }
        // Create a nullable copy of the die results to track which to keep.
        int?[] unkeptResults = new int?[quantity];
        for (int i = 0; i < quantity; i++)
        {
            unkeptResults[i] = dieResults[i];
        }
        // For each sorted result to keep...
        for (int i = 0; i < Math.Abs(keep); i++)
        {
            // Find the position of the result to keep in the array of unkept results.
            int resultToKeep = sortedResults[i];
            int pos = Array.IndexOf(unkeptResults, resultToKeep);
            // Keep that result.
            dieKeeps[pos] = true;
            // Remove that result from the unkept results.
            unkeptResults[pos] = null;
        }
        return new RollResult(dieResults, dieKeeps);
    }

    private static RollResult RollDiceF(int quantity, int keep)
    {
        if (quantity == 0)
        {
            return new RollResult(0);
        }

        int[] dieResults = new int[quantity];
        bool[] dieKeeps = new bool[quantity];

        // Roll the dice.
        for (int i = 0; i < quantity; i++)
        {
            dieResults[i] = RandomNumberGenerator.GetInt32(3) - 1;
        }

        // If keeping no dice or all of them...
        if (keep == 0 || Math.Abs(keep) >= quantity)
        {
            if (keep != 0)
            {
                for (int i = 0; i < quantity; i++)
                {
                    dieKeeps[i] = true;
                }
            }
            return new RollResult(dieResults, dieKeeps);
        }

        // Create a copy of the die results and sort them from lowest to highest.
        int[] sortedResults = (int[])dieResults.Clone();
        Array.Sort(sortedResults);
        // If keeping the highest results...
        if (keep > 0)
        {
            // Reverse the sorted results to be highest to lowest.
            Array.Reverse(sortedResults);
        }
        // Create a nullable copy of the die results to track which to keep.
        int?[] unkeptResults = new int?[quantity];
        for (int i = 0; i < quantity; i++)
        {
            unkeptResults[i] = dieResults[i];
        }
        // For each sorted result to keep...
        for (int i = 0; i < Math.Abs(keep); i++)
        {
            // Find the position of the result to keep in the array of unkept results.
            int resultToKeep = sortedResults[i];
            int pos = Array.IndexOf(unkeptResults, resultToKeep);
            // Keep that result.
            dieKeeps[pos] = true;
            // Remove that result from the unkept results.
            unkeptResults[pos] = null;
        }
        return new RollResult(dieResults, dieKeeps);
    }

    private static bool DoMath(string expression, out long result, out string remaining)
    {
        result = 0;
        remaining = expression;
        if (expression.Length == 0)
        {
            remaining = "  ";
            return false;
        }
        if (long.TryParse(expression, out result)) { return true; }

        int timeout = 0;
        while (timeout++ < 1024)
        {
            expression = MergeUnaryOperators(expression);

            if (TryMultiply(expression, out expression))
            {
                continue;
            }

            if (TryAddSubtract(expression, out expression))
            {
                continue;
            }

            if (long.TryParse(expression, out result))
            {
                return true;
            }
            else
            {
                remaining = expression;
                return false;
            }
        }
        remaining = expression;
        return false;
    }

    readonly static MatchEvaluator doubleUnary = new(match =>
            match.Value == "++" || match.Value == "--" ? "+" : "-");

    private static string MergeUnaryOperators(string input)
    {
        input = Regex.Replace(input, @"[+-][+-]", doubleUnary);
        return input;
    }

    readonly static Regex mathAddSubtract = new(@"-?\d+[+-]\d+(?=([+\-*]|$))");
    readonly static Regex mathMultiply = new(@"\d+\*-?\d+(?=([+\-*]|$))");

    private static bool TryMultiply(string expression, out string result)
    {
        bool multiplicationPerformed = false;
        Match multiplication = mathMultiply.Match(expression);
        if (multiplication.Success)
        {
            int asteriskPos = multiplication.Value.IndexOf('*');
            if (asteriskPos < 0)
            {
                result = expression;
                return false;
            }

            if (!long.TryParse(multiplication.Value[0..asteriskPos], out long numOne)
                || !long.TryParse(multiplication.Value[(asteriskPos + 1)..], out long numTwo))
            {
                result = expression;
                return false;
            }

            expression = string.Concat(expression[0..multiplication.Index],
                numOne * numTwo,
                expression[(multiplication.Index + multiplication.Value.Length)..]);

            multiplicationPerformed = true;
        }
        result = expression;
        return multiplicationPerformed;
    }

    private static bool TryAddSubtract(string expression, out string result)
    {
        bool addSubtractPerformed = false;
        Match addSubtract = mathAddSubtract.Match(expression);
        if (addSubtract.Success)
        {
            int operatorPos = addSubtract.Value.IndexOf('+');
            int sign = 1;
            if (operatorPos < 0)
            {
                if (addSubtract.Value[0] == '-')
                {
                    operatorPos = addSubtract.Value[1..].IndexOf('-') + 1;
                }
                else operatorPos = addSubtract.Value.IndexOf('-');
                sign = -1;
                if (operatorPos < 0)
                {
                    result = expression;
                    return false;
                }
            }

            if (!long.TryParse(addSubtract.Value[0..operatorPos], out long numOne)
                || !long.TryParse(addSubtract.Value[(operatorPos + 1)..], out long numTwo))
            {
                result = expression;
                return false;
            }

            expression = string.Concat(expression[0..addSubtract.Index],
                (numOne + sign * numTwo).ToString("+0;-0"),
                expression[(addSubtract.Index + addSubtract.Value.Length)..]);

            addSubtractPerformed = true;
        }
        result = expression;
        return addSubtractPerformed;
    }

    private class RollResult
    {
        internal bool Success { get; private set; }
        internal long Value { get; private set; }
        internal string Error { get; private set; }
        internal string Expression { get; set; }

        private readonly bool[] dieKeeps;
        private readonly int[] dieResults;

        public RollResult(int[] dieResults, bool[] dieKeeps)
        {
            Success = true;
            this.dieKeeps = dieKeeps;
            this.dieResults = dieResults;
            for (int i = 0; i < dieResults.Length; i++)
            {
                if (dieKeeps[i])
                {
                    Value += dieResults[i];
                }
            }

            Error = string.Empty;
            Expression = string.Empty;
        }

        public RollResult(long result)
        {
            Success = true;
            Value = result;

            Error = string.Empty;
            Expression = string.Empty;
            dieKeeps = Array.Empty<bool>();
            dieResults = Array.Empty<int>();
        }

        public RollResult(string rollExpression, string errorMessage)
        {
            Success = false;
            Error = errorMessage;
            Expression = rollExpression;

            dieKeeps = Array.Empty<bool>();
            dieResults = Array.Empty<int>();
        }

        public RollResult(string errorMessage)
        {
            Success = false;
            Error = errorMessage;

            Expression = string.Empty;
            dieKeeps = Array.Empty<bool>();
            dieResults = Array.Empty<int>();
        }

        public override string ToString()
        {
            if (!Success) { return Error; }
            else if (dieResults.Length == 0 || dieResults.Length > 20)
            {
                return Value.ToString("N0");
            }
            else
            {
                StringBuilder output = new();
                for (int i = 0; i < dieResults.Length; i++)
                {
                    if (dieKeeps[i])
                    {
                        output.Append($", **{dieResults[i].ToString()}**");
                    }
                    else
                    {
                        output.Append($", *{dieResults[i].ToString()}*");
                    }
                }
                return output.ToString(2, output.Length - 2);
            }
        }
    }
}
