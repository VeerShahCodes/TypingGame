
namespace TypingGame
{
    using Microsoft.Data.SqlClient;
    using Microsoft.Data.SqlTypes;
    using System;
    using System.Data;
    using System.Security.Cryptography;
    using System.Text;

    internal class Program
    {
        public static string CreateMD5Hash(string input)
        {
            using (MD5 mD5 = MD5.Create())
            {
                byte[] rawData = Encoding.UTF8.GetBytes(input);

                byte[] data = mD5.ComputeHash(rawData);

                StringBuilder stringBuilder = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    stringBuilder.Append(data[i].ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }

        public static int ExecuteCommand(SqlCommand sqlCommand, SqlConnection connection)
        {
            connection.Open();
            int x = sqlCommand.ExecuteNonQuery();
            connection.Close();
            return x;
        }

        //public static DataTable ReturnDataCommand(SqlCommand sqlCommand, SqlConnection connection)
        //{
        //    DataTable table = new DataTable();
        //    SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand);
        //    connection.Open();

        //    try
        //    {
        //        int x = adapter.Fill(table);

        //    }
        //    catch (SqlException ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //    finally
        //    {
        //        connection.Close();
        //    }

        //    return table.
        //}

        public static string MakeSalt()
        {
            var sb = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                sb.Append((char)random.Next(33, 126));
            }
            return sb.ToString();
        }

        public static void SignUp(SqlConnection connection)
        {
            string command = "usp_SignUp";
            SqlCommand sqlCommand = new SqlCommand(command, connection);
            sqlCommand.CommandType = CommandType.StoredProcedure;

            Console.WriteLine("Enter your desired username:");
            var username = Console.ReadLine();
            Console.WriteLine("Enter your desired password:");
            var password = Console.ReadLine();

            string salt = MakeSalt();
            string hashedPassword = CreateMD5Hash(password + salt);

            sqlCommand.Parameters.AddWithValue("@Username", username);
            sqlCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            sqlCommand.Parameters.AddWithValue("@Salt", salt);

            ExecuteCommand(sqlCommand, connection);
        }

        public static string GetSalt(SqlConnection connection, string username)
        {
            string getSalt = "usp_GetSalt";
            SqlCommand sqlCommand = new SqlCommand(getSalt, connection);
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCommand.Parameters.AddWithValue("@Username", username);
            connection.Open();
            var salt = (string)sqlCommand.ExecuteScalar();
            connection.Close();
            return salt;
        }
        public static int Login(string username, SqlConnection connection)
        {

            Console.WriteLine("Enter your password:");
            var password = Console.ReadLine();
            var salt = GetSalt(connection, username);
            var hashedPassword = CreateMD5Hash(password + salt);

            string login = "usp_Login";
            SqlCommand loginCommand = new SqlCommand(login, connection);
            loginCommand.CommandType = CommandType.StoredProcedure;

            loginCommand.Parameters.AddWithValue("@Username", username);
            loginCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            connection.Open();
            int id = (int)loginCommand.ExecuteScalar();
            connection.Close();
            return id;
        }

        public static string[] GenerateGameWords(string txtFile)
        {
            string[] fileContents = File.ReadAllLines(txtFile);

            return fileContents;
        }

        public static int GetCorrectCharacters(string original, string typed)
        {
            int count = 0;
            if (typed.Length <= original.Length)
            {
                for (int i = 0; i < typed.Length; i++)
                {
                    if (typed[i] == original[i])
                    {
                        count++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < original.Length; i++)
                {
                    if (typed[i] == original[i])
                    {
                        count++;
                    }
                }

            }

            return count;
        }

        public static void PlayGame(string[] paragraphs, Random random, int userID, SqlConnection connection)
        {
            int gameNum = random.Next(paragraphs.Length);
            Console.WriteLine(paragraphs[gameNum]);
            int wordCount = paragraphs[gameNum].Length / 5;

            Console.WriteLine("START TYPING NOW:");

            TimeSpan timeStart = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            string answer = Console.ReadLine();

            TimeSpan timeEnd = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            TimeSpan timeTotal = timeEnd.Subtract(timeStart);
            double time = timeTotal.TotalMinutes;
            double seconds = Math.Round(time * 60, 2);
            Console.WriteLine("time: " + time * 60);

            double wpm = Math.Round(wordCount / time, 2);
            Console.WriteLine("wpm: " + wordCount / time);

            double accuracy = Math.Round((double)GetCorrectCharacters(paragraphs[gameNum], answer) / paragraphs[gameNum].Length, 2);
            Console.WriteLine("accuracy: " + accuracy * 100 + "%");

            string commandText = "usp_AddGame";
            SqlCommand command = new SqlCommand(commandText, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserID", userID);
            command.Parameters.AddWithValue("@CompletionTime", seconds);
            command.Parameters.AddWithValue("@WordsPerMinute", wpm);
            command.Parameters.AddWithValue("@Accuracy", accuracy * 100);
            command.Parameters.AddWithValue("@Paragraph", paragraphs[gameNum]);
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();


        }

        public static void ShowWPMLeaderboard(SqlConnection connection)
        {
            string commandText = "usp_ShowWPMLeaderboard";
            SqlCommand command = new SqlCommand(commandText, connection);
            connection.Open();
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(reader.GetSqlSingle(reader.GetOrdinal("WordsPerMinute")));
            }
            connection.Close();
        }
        
        public static void ShowAccuracyLeaderboard(SqlConnection connection)
        {
            string commandText = "usp_ShowAccuracyLeaderboard";
            SqlCommand command = new SqlCommand( commandText, connection);
            connection.Open();
            var reader = command.ExecuteReader();
            while(reader.Read())
            {
                Console.WriteLine(reader.GetSqlSingle(reader.GetOrdinal("Accuracy")));

            }
            connection.Close();
        }


        static void Main(string[] args)
        {
            Random random = new Random();
            string ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"\\\\GMRDC1\\Folder Redirection\\Veer.Shah\\Documents\\Visual Studio 2022\\Projects\\SQLAPIs\\TypingGame\\TypingGame\\Database1.mdf\";Integrated Security=True";
            SqlConnection connection = new SqlConnection(ConnectionString);
            string txtFilePath = "\\\\GMRDC1\\Folder Redirection\\Veer.Shah\\Documents\\Visual Studio 2022\\Projects\\SQLAPIs\\TypingGame\\TypingGame\\randomParagraphs.txt";
            string[] paragraphs = GenerateGameWords(txtFilePath);
            Console.WriteLine("Welcome to the typing game...");

            while (true)
            {
                Console.WriteLine("Type L to login or S to sign up:");
                var input = Console.ReadLine();
                if (input.ToUpper() == "L")
                {
                    Console.WriteLine("Enter your username:");
                    var username = Console.ReadLine();
                    int userID = Login(username, connection);
                    if (userID > 0)
                    {
                        Console.WriteLine("Login successful");
                        while (true)
                        {
                            Console.WriteLine("Type S to sign out, Type V to view leaderboard, Type G to play a game");
                            var loginInput = Console.ReadLine();

                            if (loginInput.ToUpper() == "G")
                            {
                                PlayGame(paragraphs, random, userID, connection);
                            }
                            else if (loginInput.ToUpper() == "V")
                            {
                                Console.WriteLine("Type A to see accuracy leaderboard or type W to see wpm leaderboard");
                                var leaderboardInput = Console.ReadLine();

                                if(leaderboardInput.ToUpper() == "A")
                                {
                                    ShowAccuracyLeaderboard(connection);
                                }
                                else if(leaderboardInput.ToUpper() == "W")
                                {
                                    ShowWPMLeaderboard(connection);
                                }
                            }
                            else if (loginInput.ToUpper() == "S")
                            {
                                Console.WriteLine("Signed out");
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Bad login, try again");
                    }
                }
                else if (input.ToUpper() == "S")
                {
                    SignUp(connection);

                }
                else
                {
                    Console.WriteLine("Invalid input. Please type L to login or S to sign up.");
                }
            }


        }
    }
}
