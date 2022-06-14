namespace ArgosCA.Bot.Commands;

internal static class DiceRoller
{
    internal static string EvaluateUserInput(string input)
    {
        input = RemoveWhiteSpace(input);
        return "TODO: Implement dice roller.";
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
}
