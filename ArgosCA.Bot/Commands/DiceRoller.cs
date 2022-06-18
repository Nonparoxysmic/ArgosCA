using System;
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

            RollResult rollResult = EvaluateRoll(rollExpression.Value);

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

        return output + "Roll command in development.";
    }

    private static RollResult EvaluateRoll(string expression)
    {
        // TODO: Implement roll evaluation.
        return new RollResult();
    }

    private class RollResult
    {
        internal bool Success { get; private set; }
        internal long Value { get; private set; }
        internal string Error { get; private set; }
        internal string Expression { get; set; }

        public RollResult()
        {
            // TODO: Implement RollResult.
            Success = true;
            Value = 1234567890;
            Error = "";
            Expression = "";
        }
    }
}
