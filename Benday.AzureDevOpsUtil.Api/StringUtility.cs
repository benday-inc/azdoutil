public static class StringUtility
{
    public static string RemoveControlChars(string input)
    {
        if (ContainsControlCharacters(input) == false)
        {
            return input;
        }
        else
        {
            var output = new string(input.Where(c => !char.IsControl(c)).ToArray());

            return output;
        }
    }

    public static bool ContainsControlCharacters(string input)
    {
        if (input == null)
        {
            return false;
        }
        else if (input.Length == 0)
        {
            return false;
        }
        else
        {
            foreach (var item in input)
            {
                if (char.IsControl(item) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
