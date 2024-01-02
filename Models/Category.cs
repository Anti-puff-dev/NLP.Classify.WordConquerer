using MathUtils;


namespace NLP.Models
{
    public class Category
    {
        private bool _locked = false;
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Token> Tokens { get; set; } = new List<Token>();


        public void AddTokens(Token[] tokens)
        {
            foreach (Token token in tokens)
            {
                int index = this.Tokens.FindIndex(t => t.word == token.word);
                if (index == -1)
                {
                    Tokens.Add(new Token() { word = token.word, word_original = token.word_original, count = 1, weight = 1, position = token.position });
                }
                else
                {
                    Tokens[index].count++;

                    //Need to fine tuning (x) 
                    HashSet<int> uniquesPos = new HashSet<int>(Tokens[index].position);
                    uniquesPos.UnionWith(token.position);
                    Tokens[index].position = uniquesPos.ToArray();
                }
            }
        }



        public void Weigths()
        {
            foreach (Token token in this.Tokens)
            {
                token.weight = 1 + ((token.count) / this.Tokens.Count);
            }
        }


        public void PreAttention()
        {
            for (int i = 0; i < this.Tokens.Count; i++)
            {
                this.Tokens[i].weight = 1 + ((this.Tokens[i].count) / this.Tokens.Count);

                for (int p = 0; p < this.Tokens.Count; p++)
                {
                    int k = Math.Abs(p - i) + 1;
                    this.Tokens[i].weight += (((1 / k) * this.Tokens[p].count) / this.Tokens.Count);
                }
            }
        }





        public void Attention()
        {
            //Console.Write($"Category {this.Name}");
            /*double[,] W_Q = new double[3, 3] { { 0.2, 0.35, -0.7 }, { 0.6, -0.17, -0.3 }, { 0.27, -0.48, 0.4 } };
            double[,] W_K = new double[3, 3] { { -0.51, -0.13, 0.67 }, { -0.11, 0.29, 0.49 }, { -0.01, 0.78, -0.61 } };
            double[,] W_V = new double[3, 3] { { 0.09, -0.07, 0.8 }, { 0.5, 0.12, 0.48 }, { -0.31, 0.7, 0.33 } };*/

            double[,] W_Q = new double[,] { { 2, 0, 2 }, { 2, 0, 0 }, { 2, 1, 2 } };
            double[,] W_K = new double[,] { { 2, 2, 2 }, { 0, 2, 1 }, { 0, 1, 1 } };
            double[,] W_V = new double[,] { { 1, 1, 0 }, { 0, 1, 1 }, { 0, 0, 0 } };


            for (int p = 0; p < this.Tokens.Count; p++)
            {
                this.Tokens[p].Q = MathX.Multiply([this.Tokens[p].weight, 1, 0], W_Q);
                this.Tokens[p].K = MathX.Multiply([this.Tokens[p].weight, 1, 0], W_K);
                this.Tokens[p].V = MathX.Multiply([this.Tokens[p].weight, 1, 0], W_V);
            }


            for (int p = 0; p < this.Tokens.Count; p++)
            {
                double[] scores = new double[this.Tokens.Count];

                for (int q = 0; q < this.Tokens.Count; q++)
                {
                    scores[q] = MathX.Dot(this.Tokens[q].Q, this.Tokens[q].K) / Math.Sqrt(3);
                }


                List<double[]> attention_heads = new List<double[]>();
                double[] weights = MathX.Softmax(scores);

                for (int q = 0; q < weights.Length; q++)
                {
                    attention_heads.Add(MathX.Multiply(weights[q], this.Tokens[p].V));
                }


                double[] sum = MathX.Sum(attention_heads);
                this.Tokens[p].weight = this.Tokens[p].weight * sum[0] + this.Tokens[p].weight * sum[1] + this.Tokens[p].weight * sum[2];
            }
        }
    }
}
