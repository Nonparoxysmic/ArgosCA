using System.Text.RegularExpressions;

namespace ArgosCA.Bot.Commands;

internal static class DiceRoller
{
    readonly static Regex validExpression = new(@"^-?(\d+|\d*d(\d+|%|F)([HL]\d*)?)([+\-*](\d+|\d*d(\d+|%|F)([HL]\d*)?))*$");

    internal static string EvaluateUserInput(string input)
    {
        input = RemoveWhiteSpace(input);
        string expression = ProcessInput(input);

        string output = "Input: " + input + Environment.NewLine;
        if (validExpression.IsMatch(expression))
        {
            output += "Input is valid.";
        }
        else
        {
            output += "Input is **NOT** valid.";
        }
        return output + Environment.NewLine + "Roll command in development.";
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
}
