
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

        public static DataTable ReturnDataCommand(SqlCommand sqlCommand, SqlConnection connection)
        {
            DataTable table = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand);
            connection.Open();

            try
            {
                int x = adapter.Fill(table);

            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return table;
        }

        public static string MakeSalt()
        {
            var sb = new StringBuilder();
            var random = new Random();
            for(int i = 0; i < 10; i++)
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
            var salt = ReturnDataCommand(sqlCommand, connection).Rows[0].Field<string>(0);
            return salt;
        }
        public static bool Login(SqlConnection connection)
        {
            Console.WriteLine("Enter your username:");
            var username = Console.ReadLine();
            Console.WriteLine("Enter your password:");
            var password = Console.ReadLine();
            var salt = GetSalt(connection, username);
            var hashedPassword = CreateMD5Hash(password + salt);

            string login = "usp_Login";
            SqlCommand loginCommand = new SqlCommand(login, connection);
            loginCommand.CommandType = System.Data.CommandType.StoredProcedure;

            loginCommand.Parameters.AddWithValue("@Username", username);
            loginCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);

            if(ReturnDataCommand(loginCommand, connection).Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static string GenerateGameWords(string txtFile)
        {
            string fileContents = File.ReadAllText(txtFile);
            Console.WriteLine(fileContents);
            return fileContents;
        }

        static void Main(string[] args)
        {
            string ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"\\\\GMRDC1\\Folder Redirection\\Veer.Shah\\Documents\\Visual Studio 2022\\Projects\\SQLAPIs\\TypingGame\\TypingGame\\Database1.mdf\";Integrated Security=True";
            SqlConnection connection = new SqlConnection(ConnectionString);

            string txtFilePath = "\\\\GMRDC1\\Folder Redirection\\Veer.Shah\\Documents\\Visual Studio 2022\\Projects\\SQLAPIs\\TypingGame\\TypingGame\\randomParagraphs.txt";

            GenerateGameWords(txtFilePath);

            Console.WriteLine("Welcome to the typing game...");

            while(true)
            {
                Console.WriteLine("Type L to login or S to sign up:");
                var input = Console.ReadLine();
                if (input.ToUpper() == "L")
                {
                    if(Login(connection))
                    {
                        Console.WriteLine("Login successful");
                        while(true)
                        {
                            Console.WriteLine("Type S to sign out, Type V to view leaderboard, Type G to play a game");
                            var loginInput = Console.ReadLine();

                            if(loginInput.ToUpper() == "G")
                            {

                            }
                            else if(loginInput.ToUpper() == "V")
                            {

                            }
                            else if(loginInput.ToUpper() == "S")
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
