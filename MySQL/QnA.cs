using db = MySQL ;

namespace NLP.MySQL
{
    public class QnA
    {
        private string string_conection = "";
        private string field_name = "";
        private string table_name = "";


        public QnA()
        {
            
        }

        public QnA(string table_name, string field_name)
        {
            this.table_name = table_name;
            this.field_name = field_name;
        }

        public static QnA Instance()
        {
            return new QnA();
        }

        public static QnA Instance(string table_name, string field_name)
        {
            return new QnA(table_name, field_name);
        }



        #region Setters
        public QnA Connection(string string_conection)
        {
            this.string_conection = string_conection;
            return this;
        }


        public QnA FieldName(string field_name)
        {
            this.field_name = field_name;
            return this;
        }


        public QnA TableName(string table_name)
        {
            this.table_name = table_name;
            return this;
        }
        #endregion Setters



        public Result[] Predict(string phrase, int results = 1)
        {
            string[] list = phrase.Split(new char[] { ' ', '\t' });


            string ps = "";
            for (int i = 0; i < list.Length; i++)
            {
                ps += $"p{i}"; 
                if (i < list.Length - 1) ps += "+";
            }


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


            string positions = "";
            for (int i = 0; i < list.Length; i++)
            {
                positions += $"POSITION('{list[i]}' IN {field_name}) AS p{i}";
                if (i < list.Length - 1) positions += "," + Environment.NewLine;
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
	            *,
	            ({ps}) AS psum,
	            val/({ps}) AS confidence
            FROM

            (
            SELECT {field_name} AS phrase,  
	            {matches},
	            {positions},
	            ({vals}) AS val

            FROM {table_name}

            ORDER BY ({vals}) DESC

            LIMIT 10
            ) AS t

            ORDER BY confidence DESC LIMIT {results};
            ";

            db.DbConnection.ConnString = string_conection;
            return db.Json.Select.Fill(query, new string[] { }).Multiple<Result>();
        }
    }
}
