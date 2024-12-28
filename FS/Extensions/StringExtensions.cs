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
    }
}
