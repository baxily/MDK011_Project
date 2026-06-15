using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace SveshofReff.Data
{
    public static class DatabaseInitializer
    {
        public static string DbPath { get; } = "svezhov.db";
        public static string ConnectionString => $"Data Source={DbPath}";

        public static void Initialize()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    PhoneNumber TEXT NOT NULL,
                    ReferralCode TEXT NOT NULL UNIQUE,
                    InviterID INTEGER NULL,
                    PointsBalance INTEGER NOT NULL DEFAULT 0,
                    RegistrationDate TEXT NOT NULL,
                    FOREIGN KEY(InviterID) REFERENCES Users(ID)
                );";

            var createTransactionsTable = @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    OperationType TEXT NOT NULL,
                    PointsAmount INTEGER NOT NULL,
                    Description TEXT NOT NULL,
                    TransactionDate TEXT NOT NULL,
                    FOREIGN KEY(UserID) REFERENCES Users(ID)
                );";

            connection.Execute(createUsersTable);
            connection.Execute(createTransactionsTable);
            
            SeedData(connection);
        }

        private static void SeedData(SqliteConnection connection)
        {
            var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
            if (count == 0)
            {
                var insertUserSql = @"
                    INSERT INTO Users (FullName, PhoneNumber, ReferralCode, InviterID, PointsBalance, RegistrationDate) 
                    VALUES (@FullName, @PhoneNumber, @ReferralCode, @InviterID, @PointsBalance, @RegistrationDate);
                    SELECT last_insert_rowid();";
                var insertTransSql = "INSERT INTO Transactions (UserID, OperationType, PointsAmount, Description, TransactionDate) VALUES (@UserId, 'Начисление', @Amount, @Desc, @Date)";

                var random = new System.Random();
                var firstNames = new[] { "Иван", "Петр", "Сергей", "Алексей", "Дмитрий", "Евгений", "Александр", "Николай", "Анна", "Елена", "Мария", "Ольга", "Татьяна", "Наталья", "Виктория", "Екатерина" };
                var lastNames = new[] { "Иванов", "Петров", "Смирнов", "Соколов", "Михайлов", "Федоров", "Морозов", "Волков", "Алексеев", "Лебедев", "Семенов", "Егоров", "Павлов", "Козлов", "Степанов" };

                var userIds = new System.Collections.Generic.List<int>();

                for (int i = 1; i <= 50; i++)
                {
                    string fullName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                    string phone = "79" + random.Next(100000000, 999999999).ToString();
                    string refCode = "REF" + i.ToString("D4") + random.Next(10, 99).ToString();
                    
                    int? inviterId = null;
                    if (i > 5 && random.Next(100) < 60)
                    {
                        inviterId = userIds[random.Next(0, userIds.Count)];
                    }

                    int points = 50;
                    var regDate = System.DateTime.Now.AddDays(-random.Next(0, 30));

                    if (inviterId != null) 
                    {
                        connection.Execute("UPDATE Users SET PointsBalance = PointsBalance + 100 WHERE ID = @Id", new { Id = inviterId });
                        connection.Execute(insertTransSql, new { UserId = inviterId, Amount = 100, Desc = $"Бонус за приглашение ({fullName})", Date = regDate.ToString("yyyy-MM-dd HH:mm:ss") });
                    }

                    var userId = connection.ExecuteScalar<int>(insertUserSql, new { 
                        FullName = fullName, 
                        PhoneNumber = phone, 
                        ReferralCode = refCode, 
                        InviterID = inviterId, 
                        PointsBalance = points, 
                        RegistrationDate = regDate.ToString("yyyy-MM-dd HH:mm:ss") 
                    });

                    connection.Execute(insertTransSql, new { UserId = userId, Amount = 50, Desc = "Приветственный бонус", Date = regDate.ToString("yyyy-MM-dd HH:mm:ss") });

                    userIds.Add(userId);
                }
            }
        }
    }
}
