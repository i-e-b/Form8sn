namespace Form8snCore.Tests
{
    public class SampleData
    {
        public static object Standard => SkinnyJson.Json.Defrost(SkinnyJson.Json.Freeze(StandardSrc));
        
        public static object StandardSrc => new
        {
            Branch = "My GTRS Branch",
            BatchNumber = "792",
            Claimant = new
            {
                Name = "FirstName Surname",
                EntityType = "Bank",
                FiscalAddress = new
                {
                    HouseAptNumber = "100",
                    Street = "Fiscal Road",
                    City = "London",
                    PostalCodeZip = "NW1 99D",
                    Country = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    }
                },
                MailingAddress = new
                {
                    HouseAptNumber = "1",
                    Street = "Mailing Road",
                    City = "London",
                    PostalCodeZip = "NW1 8PD",
                    Country = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    }
                },
                LocalTaxAuthority = new
                {
                    AuthorityName = "DefaultTaxAuthority",
                    Address = new
                    {
                        HouseAptNumber = "1",
                        Street = "Mailing Road",
                        City = "London",
                        PostalCodeZip = "NW1 8PD",
                        Country = new
                        {
                            Name = "United Kingdom",
                            OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                            Code = "GBR"
                        }
                    }
                },
                LocalTaxID = "A1234"
            },
            Reclaims = new object[]
            {
                new
                {
                    Stock = new
                    {
                        ID = "Stock 1",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2003-02-01",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.010,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                },
                new
                {
                    Stock = new
                    {
                        ID = "Stock 2",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2002-02-02",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.610,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                },
                new
                {
                    Stock = new
                    {
                        ID = "Stock 3",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2003-02-01",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.610,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                },
                new
                {
                    Stock = new
                    {
                        ID = "Stock 4",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2002-02-02",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.610,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                },
                new
                {
                    Stock = new
                    {
                        ID = "Stock 5",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2002-02-02",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.610,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                },
                new
                {
                    Stock = new
                    {
                        ID = "Stock 6",
                        Name = "StockName Stock GBR",
                        Type = new
                        {
                            Code = "ORD",
                            Name = "Ordinary"
                        }
                    },
                    NumberOfShares = 55,
                    DividendRate = 12.34,
                    PayDate = "2002-02-02",
                    PaymentCurrency = "EUR",
                    Depot = new
                    {
                        Code = "Depot1",
                        AccountNumber = "123456789",
                        CustodianName = "DefaultCustodian",
                        CustodianAddress = new
                        {
                            HouseAptNumber = "1",
                            Street = "Mailing Road",
                            City = "London",
                            PostalCodeZip = "NW1 8PD",
                            Country = new
                            {
                                Name = "United Kingdom",
                                OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                                Code = "GBR"
                            }
                        }
                    },
                    ReclaimMarket = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    },
                    Amounts = new
                    {
                        GrossDividendAmount = 882.310,
                        WithholdingTaxRate = 30,
                        WithholdingTaxAmount = 203.610,
                        NetDividendAmount = 678.70,
                        ReclaimRate = 15,
                        ReclaimAmount = 132.34650
                    }
                }
            },
            ForeignTaxAuthority = new
            {
                AuthorityName = "ForeignTaxAuth",
                Address = new
                {
                    HouseAptNumber = "171",
                    Building = "100",
                    Street = "Taxation Road",
                    City = "London",
                    PostalCodeZip = "NW1 8PD",
                    Country = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    }
                }
            },
            AuthorisedRepresentative = new
            {
                CompanyName = "DefaultCompany",
                ContactPerson = "Default Contact Person",
                ContactEmail = "defaultContactEmail\u0040example\u002Ecom",
                Address = new
                {
                    HouseAptNumber = "1",
                    Street = "Mailing Road",
                    City = "London",
                    PostalCodeZip = "NW1 8PD",
                    Country = new
                    {
                        Name = "United Kingdom",
                        OfficialName = "United Kingdom of Great Britain and Northern Ireland \u0028the\u0029",
                        Code = "GBR"
                    }
                }
            }
        };
    }
}