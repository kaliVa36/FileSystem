using Constants;

namespace FS.Extensions
{
    public static class StringExtensions
    {
        public static string TrimZeroes(this string text)
        {
            char[] chars = text.ToCharArray();

            // Find the last non-zero character
            int lastIndex = text.Length - 1;
            while (lastIndex >= 0 && chars[lastIndex] == '0')
            {
                lastIndex--;
            }

            char[] newText = new char[lastIndex + 1];
            for (int i = 0; i <= lastIndex; i++)
            {
                newText[i] = chars[i];
            }

            return new string(newText);
        }

        public static string PadZeroes(this string text, int size)
        {
            char[] chars = new char[size];

            for (int i = 0; i < text.Length; i++)
            {
                chars[i] = text[i];
            }

            // Filling the rest with zeroes
            for (int i = text.Length; i < size; i++)
            {
                chars[i] = '0';
            }

            return new string(chars);
        }

        public static string AddFileName(this string path)
        {
            return path + "/" + ConstantsValues.ContainerName;
        }

        public static string[] SplitByChar(this string text, char symbol)
        {
            var result = new List<string>();
            char[] word = new char[text.Length];
            int wordIndex = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == symbol)
                {
                    if (wordIndex > 0)
                    {
                        result.Add(new string(word, 0, wordIndex));
                        wordIndex = 0;
                    }
                }
                else {
                    word[wordIndex++] = text[i];
                }
            }
            if (wordIndex > 0) 
            {
                result.Add(new string(word, 0, wordIndex));
            }
            return result.ListToArray();
        }

        public static string[] ListToArray(this List<string> list)
        {
            string[] strings = new string[list.Count];
            for (int i = 0; list.Count > i; i++)
            {
                strings[i] = list[i];
            }

            return strings;
        }

        public static string SplitLastChar(this string text, char symbol)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == symbol)
                {
                    text = text.Remove(i);
                    break;
                }
            }
            Console.WriteLine(text);
            return text;
        }
    }
}
