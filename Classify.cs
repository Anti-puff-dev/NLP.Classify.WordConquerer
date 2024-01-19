using Newtonsoft.Json;
using NLP.Enums;
using NLP.Models;
using System.Data;

namespace NLP
{
    public class Classify
    {
        private double word_pooling = 1d;
        private double dropout = 0.0001d;
        private int maxlength = 0;
        private bool soundex = false;
        private bool usePreAttention = true;
        private Modes mode = Modes.ATTENTION;

        private List<Category> categories = new List<Category>();
        private List<Tensor> tensors = new List<Tensor>();

        private string model_path = "";

        public Classify()
        {

        }

        public Classify(string model_path)
        {
            this.model_path = model_path;
        }


        public static Classify Instance()
        {
            return new Classify();
        }


        public static Classify Instance(string model_path)
        {
            return new Classify(model_path);
        }


        #region Setters
        public Classify WordPooling(double word_pooling)
        {
            this.word_pooling = word_pooling;
            return this;
        }

        public Classify MaxLength(int maxlength)
        {
            this.word_pooling = word_pooling;
            return this;
        }

        public Classify Soundex(bool soundex)
        {
            this.soundex = soundex;
            return this;
        }

        public Classify Mode(Modes mode)
        {
            this.mode = mode;
            return this;
        }


        public Classify Model(string model_path, bool read = false)
        {
            this.model_path = model_path;

            Console.WriteLine($"{model_path} {File.Exists(model_path)} {read}");

            if(File.Exists(model_path) && read)
            {
                tensors = LoadModel(model_path);
            }
            return this;
        }


        public Classify Dropout(double dropout)
        {
            this.dropout = dropout;
            return this;
        }


        public Classify UsePreAttention(bool usePreAttention)
        {
            this.usePreAttention = usePreAttention;
            return this;
        }
        #endregion Setters



        #region AddData
        public Classify AddCategory(string name, string text)
        {
            int index = categories.FindIndex(c => c.Name == name);

            if (index == -1)
            {
                Category t = new Category() { Id = categories.Count() + 1, Name = name };
                t.AddTokens(Tokenize.Instance(word_pooling, maxlength, soundex).Apply(Sanitize.HardApply(text)));
                categories.Add(t);
            }
            else
            {
                categories[index].AddTokens(Tokenize.Instance(word_pooling, maxlength, soundex).Apply(Sanitize.HardApply(text)));
            }
            return this;
        }



        public Classify AddCategories(string name, string[] list)
        {
            foreach (string item in list)
            {
                AddCategory(name, item);
            }
            return this;
        }
        #endregion AddData



        #region Train
        public void Train()
        {
            Console.WriteLine("Training Start");
            if(usePreAttention) UsePreAttention();
            if (mode == Modes.ATTENTION || mode == Modes.HARD_ATTENTION)
            {
                UseAttention();
            }
            Race();
            Fit();

            if (String.IsNullOrEmpty(model_path)) model_path = "conquerer-" + mode.ToString().ToLower() + "-" + word_pooling + "-model.bin";
            SaveModel(model_path);
            Console.WriteLine("Training Finished");
        }


        void Race()
        {
            for (int i = 0; i < categories.Count() - 1; i++)
            {
                for (int j = i + 1; j < categories.Count(); j++)
                {
                    (Category c1, Category c2) = Conquerer(categories[i], categories[j]);
                    categories[i] = c1;
                    categories[j] = c2;
                }
            }
        }


        void Fit()
        {
            tensors.Clear();
            for (int i = 0; i < categories.Count(); i++)
            {
                for (int j = 0; j < categories[i].Tokens.Count(); j++)
                {
                    tensors.Add(new Tensor() { category = categories[i].Name, word = categories[i].Tokens[j].word, weight = categories[i].Tokens[j].weight });
                }
            }
        }


        (Category, Category) Conquerer(Category c1, Category c2)
        {
            Console.Write($"Conquerer {c1.Name} {c1.Tokens.Count} {c2.Name} {c2.Tokens.Count} -> ");
            List<Token> tokens;


            tokens = Intersect(c1.Tokens, c2.Tokens);

            foreach (Token token in tokens)
            {
                int index1 = c1.Tokens.FindIndex(t => t.word == token.word);
                int index2 = c2.Tokens.FindIndex(t => t.word == token.word);

                if (index1 > -1 && index2 > -1)
                {

                    double c1Alpha = Alpha(token, tokens, c1);
                    double c2Alpha = Alpha(token, tokens, c2);

                    //Console.WriteLine($"{c1Weight} {c2Weight}");


                    if (c1Alpha > c2Alpha)
                    {
                        if (mode == Modes.HARD || mode == Modes.HARD_ATTENTION)
                        {
                            c1.Tokens[index1].weight = (c1.Tokens[index1].weight * c1Alpha) / 2;
                            c2.Tokens.RemoveAt(index2);
                        }
                        else if (mode == Modes.WEIGHT)
                        {
                            c1.Tokens[index1].weight = (c1.Tokens[index1].weight * c1Alpha) / 2;
                            c2.Tokens[index2].weight = (c2.Tokens[index2].weight * c2Alpha) / 10;
                        }
                        else if (mode == Modes.ATTENTION)
                        {
                            c1.Tokens[index1].weight = (c1.Tokens[index1].weight * c1Alpha) / 2;
                            c2.Tokens[index2].weight = (c2.Tokens[index2].weight * c2Alpha) / 10;
                        }
                    }
                    else
                    {
                        if (mode == Modes.HARD || mode == Modes.HARD_ATTENTION)
                        {
                            c2.Tokens[index2].weight = (c2.Tokens[index2].weight * c2Alpha) / 2;
                            c1.Tokens.RemoveAt(index1);
                        }
                        else if (mode == Modes.WEIGHT)
                        {
                            c2.Tokens[index2].weight = (c2.Tokens[index2].weight * c2Alpha) / 2;
                            c1.Tokens[index1].weight = (c1.Tokens[index1].weight * c1Alpha) / 10;
                        }
                        else if (mode == Modes.ATTENTION)
                        {
                            c2.Tokens[index2].weight = (c2.Tokens[index2].weight * c2Alpha) / 2;
                            c1.Tokens[index1].weight = (c1.Tokens[index1].weight * c1Alpha) / 10;
                        }
                    }
                }

            }


            Console.WriteLine($" Intersect: {tokens.Count}");
            return ((Category)c1, (Category)c2);
        }
        #endregion Train



        #region Predict
        public Result[] Predict(string text, int max_results = 2)
        {
            Token[] tokens = Tokenize.Instance(word_pooling, maxlength, soundex).Apply(Sanitize.HardApply(text));
            List<Result> results = new List<Result>();
            

            if (mode == Modes.HARD || mode == Modes.HARD_ATTENTION)
            {
                foreach (Token token in tokens)
                {
                    Tensor? t = tensors.Find(t => t.word == token.word);

                    if (t != null)
                    {
                        int index = results.FindIndex(r => r.category == t.category);
                        if (index > -1)
                        {
                            results[index].weight += (t.weight > dropout ? t.weight : 0);

                        }
                        else
                        {
                            results.Add(new Result() { category = t.category, weight = (t.weight > dropout ? t.weight : 0) });
                        }
                    }
                }
            }
            else
            {
                foreach (Token token in tokens)
                {
                    Tensor[]? ts = tensors.Where(t => t.word == token.word).ToArray();


                    if (ts != null)
                    {
                        foreach (Tensor t in ts)
                        {
                            int index = results.FindIndex(r => r.category == t.category);
                            if (index > -1)
                            {
                                results[index].weight += (t.weight > dropout ? t.weight : 0);

                            }
                            else
                            {
                                results.Add(new Result() { category = t.category, weight = (t.weight > dropout ? t.weight : 0) });
                            }
                        }
                    }
                }
            }

            results = SoftMax(results);
            results = results.OrderByDescending(r => r.confidence).ToList();
            return Limiter(results, max_results).ToArray();

        }
        #endregion Predict



        #region File Actions
        void SaveModel(string filename)
        {
            Console.WriteLine($"SaveModel: {filename} Tensors: {tensors.Count}");

            string json = JsonConvert.SerializeObject(tensors);
            File.WriteAllText(filename, json);
        }


        List<Tensor> LoadModel(string filename)
        {
            Console.WriteLine($"LoadModel: {filename}");
            StreamReader sr = new StreamReader(filename);
            string all = sr.ReadToEnd();
            return JsonConvert.DeserializeObject<List<Tensor>>(all);
        }
        #endregion File Actions



        #region Functions
        void UsePreAttention()
        {
            for (int i = 0; i < categories.Count(); i++)
            {
                categories[i].PreAttention();
            }
        }


        void UseAttention()
        {
            for (int i = 0; i < categories.Count(); i++)
            {
                categories[i].Attention();
            }
        }


        double Alpha(Token selected_token, List<Token> intersect_tokens, Category cat)
        {
            double _alpha = 0d;

            int position = cat.Tokens.FindIndex(t => t.word == selected_token.word);

            int i = 0;
            foreach (Token token in intersect_tokens)
            {
                if (token.word != selected_token.word)
                {
                    int index_cat = cat.Tokens.FindIndex(t => t.word == token.word);   
                    if (index_cat == -1) continue;

                    int near_position = AlphaMinorDistancePosition(position, cat.Tokens[index_cat].position);
                    double near_distance = Math.Abs(near_position - index_cat);
                    if (near_distance == 0) near_distance = 0.9;
                    _alpha += (1d / ((double)near_distance)) / intersect_tokens.Count;
                    i++;
                }

            }

            return _alpha;
        }


        int AlphaMinorDistancePosition(int position, int[] ids)
        {
            int dist = 10000;
            int rid = 0;
            foreach (int id in ids)
            {
                int dst = Math.Abs(position - id);
                if (dst < dist)
                {
                    dist = dst;
                    rid = id;
                }
            }
            return rid;
        }


        List<Token> Intersect(List<Token> arr1, List<Token> arr2)
        {
            List<Token> result = new List<Token>();
            foreach (Token token1 in arr1)
            {
                foreach (Token token2 in arr2)
                {
                    if (token1.word == token2.word)
                    {
                        result.Add(token1);
                    }
                }
            }

            return result;
        }


        List<Result> SoftMax(List<Result> results)
        {
            double sum = 0d;

            foreach (Result result in results)
            {
                sum += result.weight;
            }

            results = Confidences(results, sum);

            return results;
        }


        List<Result> Confidences(List<Result> results, double sum)
        {
            foreach (Result result in results)
            {
                result.confidence = result.weight / sum;
            }

            return results;
        }


        List<Result> Limiter(List<Result> results, int max)
        {
            if (results.Count() > max)
            {
                results.RemoveRange(max, results.Count() - max);
            }
            return results;

        }


        public static void Print(Result[] list)
        {
            foreach (Result result in list)
            {
                Console.WriteLine($"{result.category} {result.confidence}");
            }
        }
        #endregion Functions
    }
}
