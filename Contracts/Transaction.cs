using System;
using CsvHelper;

namespace statementReader.Contracts
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public int? Card { get; set; }
        public string Misc { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
    }
}
