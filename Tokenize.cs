using StringUtils;
using System.Text.RegularExpressions;


namespace NLP
{
    public static class Extensions
    {
        public static IEnumerable<int> AllIndexesOf(this string str, string searchstring)
        {
            int minIndex = str.IndexOf(searchstring);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = str.IndexOf(searchstring, minIndex + searchstring.Length);
            }
        }
    }



    public class Tokenize
    {
        public static double word_pooling = 1d;
        public static int maxlength = 0;
        public static bool soundex = false;

        public Tokenize()
        {

        }

        public static Tokenize Instance()
        {
            return new Tokenize();
        }


        public static Tokenize Instance(double word_pooling, int maxlength, bool sondex = false)
        {
            Tokenize.word_pooling = word_pooling;
            Tokenize.maxlength = maxlength;
            Tokenize.soundex = sondex;
            return new Tokenize();
        }



        public Models.Token[] Apply(string text)
        {
            string[] list = text.Split(new char[] { ' ', '\t' });
            var result = list.Where(x => !string.IsNullOrEmpty(x)).GroupBy(k => k, StringComparer.InvariantCultureIgnoreCase);

            List<Models.Token> tokens = new List<Models.Token>();
            int position = 0;
            foreach (var value in result)
            {
                IEnumerable<int> ids = text.AllIndexesOf(value.Key);
                string key = word_pooling < 1 ? WordPooling(value.Key, word_pooling) : value.Key;
                if (soundex)
                {
                    key = Soundex.Apply(key, maxlength);
                }

                if (maxlength > 0)
                {
                    if (key.Length > maxlength)
                    {
                        key = key.Substring(0, maxlength);
                    }
                }
                if (key.Length >= 4 && !Regex.IsMatch(key, @"^\d+$") && !Regex.IsMatch(key[0].ToString(), @"^\d+$"))
                {
                    tokens.Add(new Models.Token() { word = key, word_original = value.Key, count = value.Count(), position = ids.ToArray() });
                }
                position++;
            }

          
            return tokens.Distinct().ToArray();
        }


        public static string WordPooling(string word, double rate)
        {
            int len = word.Length;
            if (len <= 4) return word;

            int pad = (int)Math.Ceiling((double)len * rate);
            return word.Substring(0, pad);
        }
    }
}
