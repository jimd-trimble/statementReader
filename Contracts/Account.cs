using System;
using System.Collections.Generic;

namespace statementReader.Contracts
{
    public class Account
    {
        public Guid Id { get; set; }
        public long Number { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public List<Transaction> Transactions { get; set; }
        public AccountType Type { get; set; }
    }
}
