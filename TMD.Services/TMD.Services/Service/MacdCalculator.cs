using GraphQLParser;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using TMDInvestments.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TMD.Services.Service
{
    // Class to hold the results for each data point
    public class MacdResult
    {
        public DateTime Date { get; set; }
        public double ClosePrice { get; set; }
        public double MacdLine { get; set; }
        public double SignalLine { get; set; }
        public double Histogram { get; set; }
    }

    // EMA Calculator Helper Class
    public class EmaCalculator
    {
        private readonly int _period;
        private readonly double _multiplier;
        private double _emaValue;
        private bool _isInitialized;
        public int Period => _period; // Expose the period

        public EmaCalculator(int period)
        {
            _period = period;
            _multiplier = 2.0 / (_period + 1);
            _isInitialized = false;
        }

        public double Calculate(double currentValue)
        {
            if (!_isInitialized)
            {
                _emaValue = currentValue; // Initialize the first EMA with the first data point
                _isInitialized = true;
            }
            else
            {
                // EMA formula
                _emaValue = (currentValue - _emaValue) * _multiplier + _emaValue;
            }
            return _emaValue;
        }
    }

    // MACD Calculator Class
    public class MacdCalculator
    {
        private readonly EmaCalculator _fastEma;
        private readonly EmaCalculator _slowEma;
        private readonly EmaCalculator _signalEma;

        public MacdCalculator(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            _fastEma = new EmaCalculator(fastPeriod);
            _slowEma = new EmaCalculator(slowPeriod);
            _signalEma = new EmaCalculator(signalPeriod);
        }

        public List<MacdResult> CalculateMacd(List<MACDClosesDateTime> data)
        {
            var results = new List<MacdResult>();
            int dataCount = data.Count;
            // MACD only becomes valid after enough data points for the slow EMA (26 periods)
            int requiredDataPoints = _slowEma.Period;

            for (int i = 0; i < dataCount; i++)
            {
                var item = data[i];
                double fastEmaValue = _fastEma.Calculate(item.Price);
                double slowEmaValue = _slowEma.Calculate(item.Price);
                double macdLine = fastEmaValue - slowEmaValue;

                // Check if we have enough data points to start calculating the Signal EMA
                if (i >= requiredDataPoints)
                {
                    // Calculate the signal line based on the MACD values generated so far
                    // Note: The signal EMA internally manages its own initialization/warmup
                    double signalLine = _signalEma.Calculate(macdLine);
                    double histogram = macdLine - signalLine;

                    results.Add(new MacdResult
                    {
                        Date = item.Datetime,
                        ClosePrice = item.Price,
                        MacdLine = macdLine,
                        SignalLine = signalLine,
                        Histogram = histogram
                    });
                }
            }
            return results;
        }
    }

    // Example Usage
    //public class Program
    //{
    //    public static void Main()
    //    {
    //        // Example financial data (Date, Close Price)
    //        // NOTE: You need at least 26 data points for standard MACD calculation to begin.
    //        var stockData = GenerateSampleData(50); // Generates 50 days of sequential data

    //        var macdCalculator = new MacdCalculator();
    //        List<MacdResult> results = macdCalculator.CalculateMacd(stockData);

    //        Console.WriteLine($"Calculated MACD for {results.Count} data points (out of {stockData.Count} total):");
    //        foreach (var result in results)
    //        {
    //            Console.WriteLine($"Date: {result.Date.ToShortDateString()}, MACD: {result.MacdLine:F2}, Signal: {result.SignalLine:F2}, Hist: {result.Histogram:F2}");
    //        }
    //    }

    //    // Helper function to generate sample data for demonstration
    //    public static List<(DateTime Date, double Price)> GenerateSampleData(int count)
    //    {
    //        var data = new List<(DateTime Date, double Price)>();
    //        double currentPrice = 100.0;
    //        Random rand = new Random();
    //        for (int i = 0; i < count; i++)
    //        {
    //            data.Add((DateTime.Now.AddDays(i), currentPrice));
    //            // Simulate price fluctuation slightly
    //            currentPrice += rand.NextDouble() * 2 - 1;
    //        }
    //        return data;
    //    }
    //}
}
