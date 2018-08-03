using System;
using System.Drawing.Text;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using statementReader.Contracts;
using org.pdfclown.files;
using org.pdfclown.tools;
using org.pdfclown.documents.contents;
using org.pdfclown.documents;

namespace statementReader.Business
{
    public class StatementExtractor
    {
        private string account = "9610";
        private const string Flag1 = "Date Description Check";
        private const string Begining = "Beginning balance on ";
        private const string Eof = "Ending balance on ";
        private string filePath = "";


        private int GetYear(IList<ITextString> pageTextStrings)
        {            
            const string yearHeader = "Statement Billing Period ";
            var year = 1977;

            var yearText = pageTextStrings.FirstOrDefault(x => x.Text.Contains(yearHeader));
            if (yearText == null)
            {
                return year;
            }
            
            var headerSplit = pageTextStrings[pageTextStrings.IndexOf(yearText)].Text.Split('/').ToList();
            if (headerSplit.Count > 0)
            {
                int.TryParse(headerSplit[headerSplit.Count - 1], out year);
            }
            return year;
        }
        

        private List<Transaction> PageIterator(Pages pages, string accountFlag)
        {
            var extractor = new TextExtractor();
            var transactions = new List<Transaction>();
            foreach (var page in pages)
            {
                IList<ITextString> pageTextStrings;
                try
                {
                    var tst1 = extractor.Extract(page);
                    pageTextStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }

                if (!pageTextStrings.Any(x => x.Text.Contains(accountFlag)))
                {
                    continue;
                }

                var year = GetYear(pageTextStrings);
                transactions.AddRange(TextIterator(pageTextStrings, year));
            }

            return transactions;
        }

        private IEnumerable<Transaction> TextIterator(IList<ITextString> pageTextStrings, int year)
        {
           var statementInfo = new CreditSectionInfo(pageTextStrings);
           if (statementInfo.LastPage() && statementInfo.CurrentSectionType == SectionType.None)
           {
               return new Transaction[]{};
           }

            var idx = statementInfo.CurrentSectionIdx;
            if (idx == -1)
            {
                throw new Exception("Could not determine where to start!");
            }

            var transactions = new List<Transaction>();
            Transaction transactionNowAndPrevious = null;
            while(idx > -1)
            {
                var textString = statementInfo.pageTextStrings[idx];
                var textParts = textString.Text.Trim().Split(' ').ToList();
                if (textParts.Any(x => x.Contains(CreditSectionInfo.EndOfSectionFlag)))
                {
                    idx = statementInfo.GetNextIdx();
                    continue;
                }

                if (textParts.Count < 2)
                {
                    idx++;
                    continue;
                }
                
                // Trans [07/24] | Post [07/26] | Reference Number | Description | Credits | Charges
                var dateTest = DateTime.MinValue;
                decimal amnt = 0;
                var couldParse = false;
                for (var n = textParts.Count- 1; n > 0; n--)
                {
                    couldParse = decimal.TryParse(textParts[n], out amnt);
                    if (couldParse)
                    {
                        break;
                    }
                }

                if (couldParse)
                {
                    var desc = textParts.ToList();
                    if (textParts[0].Contains("/"))
                    {
                        var dateString = $"{textParts[0]}/{year}";
                        DateTime.TryParse(dateString, out dateTest);
                        desc.RemoveAt(0);
                        desc.RemoveAt(0);
                    }

                    desc.RemoveAt(textParts.Count - 1);
                    desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

//                    var tstChrs = textString.TextChars.Where(y => y.Virtual).ToList();
//                    var tst = tstChrs.Count > 2 ? tstChrs[tstChrs.Count - 3] : tstChrs[0];
//                    var typ = tst.Box.Right < 433 ? TransactionType.Deposit : TransactionType.Withdrawal;

                    var transType = TransactionType.Deposit;
                    switch (statementInfo.CurrentSectionType)
                    {
                        case SectionType.Purchases:
                        case SectionType.Fees:
                            transType = TransactionType.Withdrawal;
                            break;
                        case SectionType.Interest:
                            transType = TransactionType.Interest;
                            break;
                        case SectionType.Payments:
                            break;
                        case SectionType.Credits:
                            break;
                        case SectionType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    transactionNowAndPrevious = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        Amount = amnt,
                        Date = dateTest,
                        Description = string.Join(" ", desc),
                        Type = transType
                    };
                }
                
                // For transactions with descriptions that "wrap" across lines
                else
                {
                    int? card = null;
                    var desc = textString.Text.Trim().Split(' ').ToList();
                    if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim()
                            .Length == 4) && int.TryParse(desc[desc.Count - 1], out var cardTst))
                    {
                        card = cardTst;
                    }

                    transactionNowAndPrevious = transactionNowAndPrevious ?? new Transaction();
                    transactionNowAndPrevious.Card = card;
                    transactionNowAndPrevious.Description += $" {textString.Text}";
                    transactions.Add(transactionNowAndPrevious);
                }
                idx++;
            }
            return transactions;
        }

        private List<Transaction> ExtractCredit(Document document)
        {
            if (account.Length < 5)
            {
                throw new Exception("Invalid account number, must be longer than 5 digits.");
            }

            var last4 = account.Substring(account.Length - 4);
            var accountFlag = $"Ending in {last4}";
            return PageIterator(document.Pages, accountFlag);
        }
        
#region Main Extract Method.

        private List<Transaction> Extract(Document document)
        {
            var extractor = new TextExtractor();
            var inProgress = false;
            var transactions = new List<Transaction>();
//            var pgCnt = 0;
            var year = 1977;
            const string transactionsStart = Flag1;

            foreach (var page in document.Pages)
            {
//                pgCnt++;
                IList<ITextString> pageTextStrings;
                try
                {
                    pageTextStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                }
                catch
                {
                    continue;
                }
                var headerSplit = pageTextStrings[0].Text.Split(' ').ToList();
                var dashIdx = headerSplit.IndexOf("-");
                if (dashIdx > -1)
                {
                    int.TryParse(headerSplit[dashIdx - 1], out year);
                }
                var accountPage = pageTextStrings.Any(x => x.Text.Contains(account));
                var hasBalances = pageTextStrings.Any(x => x.Text.Contains(Begining));

                inProgress = (!inProgress && accountPage && hasBalances) || inProgress;

                if (!inProgress)
                {
                    continue;
                }

                var hasEof = pageTextStrings.Any(y => y.Text.Contains(Eof));
                inProgress = !hasEof;

                int startIdx;
                var contText = pageTextStrings.FirstOrDefault(y => y.Text.Contains(transactionsStart));
                if (hasBalances)
                {
                    startIdx = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(Begining))) + 1;
                }
                else if (contText != null)
                {
                    startIdx = pageTextStrings.IndexOf(contText) + 1;
                }
                else
                {
                    throw new Exception("Could not determine where to start!");
                }

                Transaction transactionNowAndPrevious = null;
                for (var i = startIdx; i < pageTextStrings.Count; i++)
                {
                    var textString = pageTextStrings[i];
                    var textParts = textString.Text.Trim().Split(' ');
                    if (textParts.Length == 1 && !string.IsNullOrEmpty(textParts[0]) && long.TryParse(textParts[0], out _))
                    {
                        // end of page
                        continue;
                    }
                    if (textString.Text.Contains(Eof))
                    {
                        break;
                    }

                    var dateTest = DateTime.MinValue;
                    if (textParts.Length > 1 && textParts[textParts.Length - 2].Contains(".") && decimal.TryParse(textParts[textParts.Length - 2], out var amnt) && decimal.TryParse(textParts[textParts.Length - 1], out _))
                    {
                        if (textParts[0].Contains("/"))
                        {
                            var dateString = $"{textParts[0]}/{year}";
                            DateTime.TryParse(dateString, out dateTest);
                        }

                        var desc = textParts.ToList();
                        desc.RemoveAt(textParts.Length - 1);
                        desc.RemoveAt(textParts.Length - 2);
                        desc.RemoveAt(0);
                        desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

                        var tstChrs = textString.TextChars.Where(y => y.Virtual).ToList();
                        var tst = tstChrs.Count > 2 ? tstChrs[tstChrs.Count - 3] : tstChrs[0];
                        var typ = tst.Box.Right < 433 ? TransactionType.Deposit : TransactionType.Withdrawal;

                        int? card = null;
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out var cardTst))
                        {
                            card = cardTst;
                        }

                        transactionNowAndPrevious = new Transaction
                        {
                            Id = Guid.NewGuid(),
                            Amount = typ == TransactionType.Deposit ? amnt : amnt * -1,
                            Date = dateTest,
                            Description = string.Join(" ", desc),
                            Type = typ,
                            Card = card
                        };

                    }
                    else if (textParts.Length > textParts.Length - 1)
                        if (textParts.Length > textParts.Length - 1)
                            if (textParts.Length > 0 && decimal.TryParse(textParts[textParts.Length - 1], out amnt) &&
                                DateTime.TryParse($"{textParts[0]}/{year}", out dateTest))
                            {
                                // No balance
                                var desc = textParts.ToList();
                                desc.RemoveAt(textParts.Length - 1);
                                desc.RemoveAt(0);
                                desc = new List<string>();
                                foreach (var x in desc) desc.Add(x.Trim().Replace(',', '|'));

                                int? card = null;
                                if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" ||
                                     desc[desc.Count - 1].Trim().Length == 4) &&
                                    int.TryParse(desc[desc.Count - 1], out var cardTst))
                                {
                                    card = cardTst;
                                }

                                var tstChrs = textString.TextChars.Where(y => y.Virtual).ToList();
                                var tst = tstChrs.Count > 1 ? tstChrs[tstChrs.Count - 2] : tstChrs[0];
                                var typ = tst.Box.Right < 433 ? TransactionType.Deposit : TransactionType.Withdrawal;

                                transactionNowAndPrevious = new Transaction
                                {
                                    Id = Guid.NewGuid(),
                                    Amount = typ == TransactionType.Deposit ? amnt : amnt * -1,
                                    Date = dateTest,
                                    Description = string.Join(" ", desc),
                                    Type = typ,
                                    Card = card
                                };
                            }
                            else
                            {
                                int? card = null;
                                var desc = textString.Text.Trim().Split(' ').ToList();
                                if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out var cardTst))
                                {
                                    card = cardTst;
                                }

                                transactionNowAndPrevious = transactionNowAndPrevious ?? new Transaction();
                                transactionNowAndPrevious.Card = card;
                                transactionNowAndPrevious.Description += $" {textString.Text}";
                                transactions.Add(transactionNowAndPrevious);
                            }
                }
            }
            return transactions;
        }
#endregion

        public IEnumerable<Transaction> Run(string pAccount, string pFilePath, bool creditStatment = false)
        {
            account = pAccount;
            filePath = pFilePath;
            List<Transaction> retVal;
            using (var file = new File(filePath))
            {
                var document = file.Document;

                retVal = creditStatment
                    ? ExtractCredit(document)
                    : Extract(document);
            }
            return retVal;
        }
        
    }
}
