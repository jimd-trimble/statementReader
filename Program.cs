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
        private static long jimPersonal = 7427296830;
        private static long joint = 7427299313;
        private static long savings1 = 7217495436; //dillon j savings, JOINT
        private static long savings2 = 3650; // unknown brokerage accnt b/t Katiana & Chris, opened 12/18/2013. Ed passed spring 2015...
        private static long katCredit = 4465420198396160;
        private static List<Account> allAccounts;
        private static string year = "2018";
        private static bool creditOnly = true;

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

                Console.WriteLine("Press any key exit.");
                Console.ReadKey(true);

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
                Name = "KatCredit",
                Number = katCredit,
                Owner = "Katiana",
                Type = AccountType.Credit,
                Transactions = new List<Transaction>()
            });

            if (creditOnly)
            {
                return;
            }
            
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
//            docPath = $"/Users/jimmyd/Dev/statementReader/";
            docPath = $"/Users/jimmyd/Documents/Divorce/katCredit/{year}/";
            var txt = new StatementExtractor();
            var files = Directory.GetFiles(docPath, "*pdf").ToList();
            var all = Directory.GetFiles(docPath);
            foreach(var f in files)
            {
                foreach(var a in allAccounts)
                {
                    a.Transactions.AddRange(txt.Run(a.Number.ToString(), f, a.Type == AccountType.Credit));
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
            var outputCnt = 0;
            var outAccnts = allAccounts.Where(x => x.Transactions.Any()).ToList();
            foreach (var a in outAccnts)
            {
                var outputFile = docPath + $"WF_{year}_{a.Name}.csv";
                using (var writer = new StreamWriter(outputFile))
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "d" };
                    csv.WriteRecords(a.Transactions);
                    writer.Flush();
                    outputCnt++;
                    Console.WriteLine($"Successfully wrote file {outputFile}");
                }
            }
            Console.WriteLine($"{outputCnt} of {outAccnts.Count} created. End of program");
        }
    }
}
