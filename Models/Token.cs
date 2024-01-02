namespace NLP.Models
{
    public class Token
    {
        public string word { get; set; }
        public string word_original { get; set; }
        public double count { get; set; }
        public double weight { get; set; }
        public int[] position { get; set; }
        public double[] Q { get; set; }
        public double[] K { get; set; }
        public double[] V { get; set; }
    }
}
