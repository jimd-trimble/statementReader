using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CsvHelper;
using statementReader.Contracts;
using statementReader.Business;

namespace statementReader
{
    class MainClass
    {
        private static Parser parser;
        private static string docPath;
        //private static int docCount;
        private static long katPersonal = 9610;
        private static long jimPersonal = 6830;
        private static long joint = 9313;
        private static long savings1 = 5436;
        private static long savings2 = 3650;
        private static List<Account> allAccounts;
        private static string year = "2018";

        public static int Main(string[] args)
        {
            try
            {
                parseArgs(args);
                if (!findDoc())
                {
                    throw new ArgumentException($"Cannot find file {docPath}");
                }
                allAccounts = new List<Account>();
                SetUpAccounts();
                GetTransactions();
                writeDataToCsv();

                return 0;
            }
            catch (Exception e)
            {
                if (e != null && !string.IsNullOrEmpty(e.Message))
                {
                    Console.WriteLine(e.Message);
                }
                return 1;
            }
        }

        private static void SetUpAccounts(bool includeSavings=false)
        {
            allAccounts.Add(new Account
            {
                Id = Guid.NewGuid(),
                Name = "KatPersonal",
                Number = katPersonal,
                Owner = "Katiana",
                Type = AccountType.Checking,
                Transactions = new List<Transaction>()
            });

            allAccounts.Add(new Account
            {
                Id = Guid.NewGuid(),
                Name = "JimPersonal",
                Number = jimPersonal,
                Owner = "Jim",
                Type = AccountType.Checking,
                Transactions = new List<Transaction>()
            });

            allAccounts.Add(new Account
            {
                Id = Guid.NewGuid(),
                Name = "Joint",
                Number = joint,
                Owner = "Both",
                Type = AccountType.Checking,
                Transactions = new List<Transaction>()
            });
            if (includeSavings)
            {
                allAccounts.Add(new Account
                {
                    Id = Guid.NewGuid(),
                    Name = "Savings01",
                    Number = savings1,
                    Owner = "Both",
                    Type = AccountType.Savings,
                    Transactions = new List<Transaction>()
                });

                allAccounts.Add(new Account
                {
                    Id = Guid.NewGuid(),
                    Name = "Savings02",
                    Number = savings2,
                    Owner = "Both",
                    Type = AccountType.Savings,
                    Transactions = new List<Transaction>()
                });
            }
        }

        private static void GetTransactions()
        {
            docPath = $"/Users/jimmyd/Documents/Divorce/{year}/";
            var txt = new StatementExtractor();
            var files = Directory.GetFiles(docPath, "*pdf").ToList();
            var all = Directory.GetFiles(docPath);
            foreach(var f in files)
            {
                foreach(var a in allAccounts)
                {
                    a.Transactions.AddRange(txt.Run(a.Number.ToString(), f));
                }
            }
        }

        private static void parseArgs(string[] args)
        {
            docPath = args[0];

            // ToDo: Implement parser!
            parser = new Parser();
            var pathVal = new ValueAttribute(0);
            CommandLine.ParserSettings settings = new ParserSettings();
            CommandLine.OptionAttribute opt1 = new OptionAttribute();
        }

        private static bool findDoc()
        {
            return true;
        }

        private static void writeDataToCsv()
        {
            foreach (var a in allAccounts)
            {
                if (a.Transactions == null || !a.Transactions.Any())
                {
                    continue;
                }
                using (var writer = new System.IO.StreamWriter(docPath + $"WF_{year}_{a.Name}.csv"))
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "d" };
                    csv.WriteRecords(a.Transactions);
                    writer.Flush();
                }
            }
        }

    }
}
