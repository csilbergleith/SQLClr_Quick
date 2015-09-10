using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

public partial class UserDefinedFunctions
{
    [SqlFunction]
    public static SqlString Greeting(SqlString name)
    {

        // Put your code here
        return new SqlString ("Howdy " + (name.IsNull ? "Pardner" : (string) name));
    }

    [SqlFunction]
    public static SqlBoolean RegExMatch(SqlChars search, SqlString pattern)
    {
        Regex regex = new Regex(pattern.Value);
        return regex.IsMatch(new string(search.Value));
    }

    [SqlFunction(DataAccess= DataAccessKind.None,
                 FillRowMethodName = "FillWordRow",     // Tell SQL Server what function to call to rill a row
                 TableDefinition = "word nvarchar(300)")] // return single column as a table valued funtion
    public static IEnumerator wordsToTable(SqlString wordList) // must return IEnumerator
    {
        return new WordList((string)wordList);
    }

    public static void FillWordRow(object WordRow, out string Word)
    {
        Word = WordRow == null ? null : (string)WordRow;
    }

    public partial class WordList : IEnumerator
    {
        private string[] Words;
        private int wordPosition;
        private char[] delimiters = { ',', ' ', ':', ';' };

        public WordList(string wordList)
        {
            this.wordPosition = -1;
            try
            {
                this.Words = wordList.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                this.Words = null;
            }
        }

        public Object Current
        {
            get { return this.Words == null || wordPosition == -1 ? null : Words[wordPosition]; }
        }

        public bool MoveNext()
        {
            if (this.Words == null)
                return false;

            if (this.wordPosition >= (this.Words.Length - 1))
                return false;

            this.wordPosition++;
            return true;
        }

        public void Reset()
        {
            this.wordPosition = -1;
        }
    }

    [SqlFunction(DataAccess=DataAccessKind.Read)]
    public static SqlChars ProductToJson(SqlInt32 ID)
    {
        StringBuilder sb = new StringBuilder("");
        string template = "{{\"ProductID\":\"{0}\",\"Name\":\"{1}\"," +
                            "\"Details\":{{\"StandardCost\":\"{2}\"," +
                            "\"ListPrice\":\"{3}\"}}}}";
        if (ID.IsNull)
            return new SqlChars("");

        using (SqlConnection sqlConn = new SqlConnection("context connection=true"))
        {
            sqlConn.Open();
            using (SqlCommand cmd = sqlConn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 " +
                      " ProductId, Name, StandardCost, ListPrice From Production.Product" +
                      " where ProductID = " + ID;
                cmd.CommandType = CommandType.Text;

                SqlDataReader dr = cmd.ExecuteReader();

                dr.Read();

                if (dr.HasRows)
                {
                    sb.AppendFormat(template,
                        dr.IsDBNull(0) ? "" : dr.GetInt32(0).ToString(),
                        dr.IsDBNull(1) ? "" : dr.GetString(1),
                        dr.IsDBNull(2) ? "" : dr.GetDecimal(2).ToString(),
                        dr.IsDBNull(3) ? "" : dr.GetDecimal(3).ToString()
                        );
                }

                dr.Close();
            }
        }

        return new SqlChars(sb.ToString());
    }
}
