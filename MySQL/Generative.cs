using db = MySQL;


namespace NLP.MySQL
{
    public class Generative
    {
        private string string_conection = "";
        private string field_name = "";
        private string table_name = "";
        private double threshold = 5;

        public Generative()
        {

        }

        public Generative(string table_name, string field_name)
        {
            this.table_name = table_name;
            this.field_name = field_name;
        }

        public static Generative Instance()
        {
            return new Generative();
        }

        public static Generative Instance(string table_name, string field_name)
        {
            return new Generative(table_name, field_name);
        }



        #region Setters
        public Generative Connection(string string_conection)
        {
            this.string_conection = string_conection;
            return this;
        }


        public Generative FieldName(string field_name)
        {
            this.field_name = field_name;
            return this;
        }


        public Generative TableName(string table_name)
        {
            this.table_name = table_name;
            return this;
        }


        public Generative Threshold(double threshold)
        {
            this.threshold = threshold;
            return this;
        }
        #endregion Setters



        public Result[] NextWord(string phrase, int results = 1)
        {
            phrase = Sanitize.SoftApply(phrase);
            string[] list = phrase.Split(new char[] { ' ', '\t' });


            string sets = "";
            for (int i = 0; i < list.Length; i++)
            {
                sets += $"SET @m{i}:='{list[i]}*';" + Environment.NewLine;
            }


            string matches = "";
            for (int i = 0; i < list.Length; i++)
            {
                matches += $"(MATCH({field_name}) AGAINST(@m{i} IN BOOLEAN MODE)) AS m{i}";
                if (i < list.Length - 1) matches += "," + Environment.NewLine;
            }


            string vals = "";
            for (int i = 0; i < list.Length; i++)
            {
                vals += $"(1+(MATCH({field_name}) AGAINST(@m{i} IN BOOLEAN MODE)))";
                if (i < list.Length - 1) vals += " * " + Environment.NewLine;
            }


            string query = @$"
                {sets}  
                
                SELECT 
                    SUBSTRING_INDEX(SUBSTRING_INDEX(TRIM(BOTH ' ' FROM info), ' ', 4),' ', -1) AS phrase,
	                ({vals}) AS confidence

                FROM {table_name}

                WHERE  ({vals})>{threshold} 

                ORDER BY ({vals}) DESC LIMIT {results};  
            ";

            db.DbConnection.ConnString = string_conection;
            return db.Json.Select.Fill(query, new string[] { }).Multiple<Result>();
        }


        public Result[] Seq2Seq(string phrase, int results = 1, string eos = ".")
        {
            phrase = Sanitize.SoftApply(phrase);
            string[] list = phrase.Split(new char[] { ' ', '\t' });


            string sets = "";
            for (int i = 0; i < list.Length; i++)
            {
                sets += $"SET @m{i}:='{list[i]}*';" + Environment.NewLine;
            }


            string matches = "";
            for (int i = 0; i < list.Length; i++)
            {
                matches += $"(MATCH({field_name}) AGAINST(@m{i} IN BOOLEAN MODE)) AS m{i}";
                if (i < list.Length - 1) matches += "," + Environment.NewLine;
            }


            string vals = "";
            for (int i = 0; i < list.Length; i++)
            {
                vals += $"(1+(MATCH({field_name}) AGAINST(@m{i} IN BOOLEAN MODE)))";
                if (i < list.Length - 1) vals += " * " + Environment.NewLine;
            }



            string query = @$"
                {sets} 

                SELECT 
                    RIGHT(info, LOCATE('{eos}',  info)-(LOCATE(' {list[list.Length - 1]}', info)+LENGTH(' {list[list.Length - 1]}'))) AS phrase, 
                    ({vals}) AS confidence

                FROM {table_name}

                WHERE  ({vals})>{threshold} 

                ORDER BY ({vals}) DESC LIMIT {results}; 
            ";


            db.DbConnection.ConnString = string_conection;
            return db.Json.Select.Fill(query, new string[] { }).Multiple<Result>();
        }
    }
}
