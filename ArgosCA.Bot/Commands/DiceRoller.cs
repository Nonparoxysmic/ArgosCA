using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ArgosCA.Bot.Commands;

internal static class DiceRoller
{
    readonly static int diceLimit = 1_000_000;
    readonly static Regex diceCount = new(@"\d*(?=d)");
    readonly static Regex diceRoll = new(@"\d*d(\d+|%|F)([HL]\d*)?(?=([+\-*]|$))");
    readonly static Regex validExpression = new(@"^-?(\d+|\d*d(\d+|%|F)([HL]\d*)?)([+\-*](\d+|\d*d(\d+|%|F)([HL]\d*)?))*$");
    readonly static string NL = Environment.NewLine;

    internal static string EvaluateUserInput(string input)
    {
        input = RemoveWhiteSpace(input);
        string output = $"Input: `{input}`{NL}";
        string expression = ProcessInput(input);
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
            if (diceToRoll > (diceLimit - diceRolled))
            {
                return $"Error: Cannot evaluate expression.{NL}(Too many dice.)";
            }
            diceRolled += diceToRoll;

            RollResult rollResult = EvaluateRoll(rollExpression.Value, diceToRoll);

            if (!rollResult.Success)
            {
                return $"Error: {rollResult.Error}";
            }

            // TODO: Add individual results to output.

            expression = string.Concat(expression[0..rollExpression.Index],
                rollResult.Value.ToString(),
                expression[(rollExpression.Index + rollExpression.Value.Length)..]);
        }

        // TODO: Do math and return result.

        return output + $"Final: `{expression}`{NL}Roll command in development.";
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
            // TODO: Roll F dice.
            return new RollResult(expression, "F dice not yet implemented.");
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
        for (int i = 0; i < quantity; i++)
        {
            dieResults[i] = RandomNumberGenerator.GetInt32(faces) + 1;
        }

        bool[] dieKeeps = new bool[quantity];
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

        // TODO: Implement keeping only some of the dice.
        return new RollResult("Partial keeping not yet implemented.");
    }

    private class RollResult
    {
        internal bool Success { get; private set; }
        internal long Value { get; private set; }
        internal string Error { get; private set; }
        internal string Expression { get; set; }

        private readonly bool[] dieKeeps;
        private readonly int diceKept;
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
                    diceKept++;
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
    }
}
