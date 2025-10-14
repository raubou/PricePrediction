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
    //The MACD(Moving Average Convergence Divergence) formula involves calculating two exponential moving averages(EMAs) and then finding the difference between them.Here's the step-by-step formula:
    //Calculate the 26-period EMA:
    //EMA26 = (Close* 2/(26+1)) + (Previous EMA26 * (1 - 2/(26+1)))
    //Calculate the 12-period EMA:
    //EMA12 = (Close* 2/(12+1)) + (Previous EMA12 * 1 - 2/(12+1))
    //Calculate the MACD line:
    //MACD = EMA12 - EMA26
    //Calculate the signal line(9-period EMA of MACD) :
    //Signal = (MACD* 2/(9+1)) + (Previous Signal * (1 - 2/(9+1)))
    //You can use these formulas to calculate the MACD for any given set of closing prices.

    //This code defines a MacdCalculator class that calculates the MACD, signal, and histogram 
    //for a given list of closing prices.The CalculateMacd method returns a tuple containing the 
    //MACD, signal, and histogram values.The CalculateEma method calculates the exponential moving
    //average for a given list of values and period.The Main method demonstrates how to use the 
    //MacdCalculator class to calculate the MACD for a sample list of closing prices.
    public sealed class MacdCalculator
    {
        private readonly int _shortPeriod;
        private readonly int _longPeriod;
        private readonly int _signalPeriod;

        public MacdCalculator(int shortPeriod = 12, int longPeriod = 26, int signalPeriod = 9)
        {
            _shortPeriod = shortPeriod;
            _longPeriod = longPeriod;
            _signalPeriod = signalPeriod;
        }

        //public (List<double> Macd, List<double> Signal, List<double> Histogram) CalculateMacd(List<double> closes)
        //{
        //    var emaShort = CalculateEma(closes, _shortPeriod);
        //    var emaLong = CalculateEma(closes, _longPeriod);
        //    var macd = emaShort.Zip(emaLong, (s, l) => s - l).ToList();
        //    var signal = CalculateEma(macd, _signalPeriod);
        //    var histogram = macd.Zip(signal, (m, s) => m - s).ToList();

        //    return (macd, signal, histogram);
        //}

        //The issue is likely due to the fact that the signal and histogram calculations rely on the MACD values,
        //which are not fully calculated until a certain number of periods have passed.
        //Here's an updated version of the code that should correctly calculate the signal and histogram values:
        //public (List<double> Macd, List<double> Signal, List<double> Histogram) CalculateMacd(List<double> closes)
        public (List<MACDClosesDateTime> Macd, List<MACDClosesDateTime> Signal, List<MACDClosesDateTime> Histogram) CalculateMacd(List<MACDClosesDateTime> closes)
        {
            var emaShort = CalculateEma(closes, _shortPeriod);
            var emaLong = CalculateEma(closes, _longPeriod);
            // Replace this line:
            // var macd = emaShort.Zip(emaLong, (s, l) => s - l).ToList();
            // With the following code to subtract the Close values and keep the DateTime:
            var macd = emaShort.Zip(emaLong, (s, l) => new MACDClosesDateTime
            {
                Close = s.Close - l.Close,
                Datetime = s.Datetime // or l.DateTime, assuming they are aligned
            }).ToList();
            //var macd = emaShort.Zip(emaLong, (s, l) => s - l).ToList();
            var signal = CalculateEma(macd.Skip(Math.Max(_longPeriod - 1, 0)).ToList(), _signalPeriod);

            var histogram = signal.Select((s, i) => new MACDClosesDateTime
            {
                Close = macd[i + Math.Max(_longPeriod - 1, 0)].Close - s.Close,
                Datetime = macd[i + Math.Max(_longPeriod - 1, 0)].Datetime // keep the original date
            }).ToList();
            //var histogram = signal.Select((s, i) => macd[i + Math.Max(_longPeriod - 1, 0)] - s).ToList();
            //var macdPadded = new List<double>();
            //var signalPadded = new List<double>();
            //var histogramPadded = new List<double>();
            var macdPadded = new List<MACDClosesDateTime>();
            var signalPadded = new List<MACDClosesDateTime>();
            var histogramPadded = new List<MACDClosesDateTime>();

            for (int i = 0; i < closes.Count; i++)
            {
                if (i < Math.Max(_longPeriod - 1, 0))
                {
                    var macdClosinesDatetime = new MACDClosesDateTime
                    {
                        Close = double.NaN, // or some other value to indicate not enough data
                        Datetime = closes[i].Datetime // keep the original date
                    };
                    //macdPadded.Add(double.NaN);
                    //signalPadded.Add(double.NaN);
                    //histogramPadded.Add(double.NaN);
                    macdPadded.Add(macdClosinesDatetime);
                    signalPadded.Add(macdClosinesDatetime);
                    histogramPadded.Add(macdClosinesDatetime);
                }
                else if (i < Math.Max(_longPeriod + _signalPeriod - 2, 0))
                {
                    macdPadded.Add(macd[i - Math.Max(_longPeriod - 1, 0)]);
                    var macdClosinesDatetime = new MACDClosesDateTime
                    {
                        Close = double.NaN, // or some other value to indicate not enough data
                        Datetime = closes[i].Datetime // keep the original date
                    };
                    //signalPadded.Add(double.NaN);
                    //histogramPadded.Add(double.NaN);
                    signalPadded.Add(macdClosinesDatetime);
                    histogramPadded.Add(macdClosinesDatetime);
                }
                else
                {
                    macdPadded.Add(macd[i - Math.Max(_longPeriod - 1, 0)]);
                    signalPadded.Add(signal[i - Math.Max(_longPeriod + _signalPeriod - 2, 0)]);
                    histogramPadded.Add(histogram[i - Math.Max(_longPeriod + _signalPeriod - 2, 0)]);
                }
            }

            return (macdPadded, signalPadded, histogramPadded);
        }
        //private List<double> CalculateEma(List<double> values, int period)
        private List<MACDClosesDateTime> CalculateEma(List<MACDClosesDateTime> values, int period)
        {
            //var ema = new List<double>();
            var ema = new List<MACDClosesDateTime>();
            var multiplier = 2.0 / (period + 1);

            for (int i = 0; i < values.Count; i++)
            {
                if (i < period - 1)
                {
                    var macdClosinesDatetime = new MACDClosesDateTime
                    {
                        Close = double.NaN, // or some other value to indicate not enough data
                        Datetime = values[i].Datetime // keep the original date
                    };  
                    //ema.Add(double.NaN); // or some other value to indicate not enough data
                    ema.Add(macdClosinesDatetime); // or some other value to indicate not enough data
                }
                else if (i == period - 1)
                {
                    var macdClosinesDatetime = new MACDClosesDateTime
                    {
                        Close = values.Select(x => x.Close).ToList().GetRange(0, period).Average(), // or some other value to indicate not enough data
                        Datetime = values[i].Datetime // keep the original date
                    };
                    //ema.Add(values.GetRange(0, period).Average());
                    ema.Add(macdClosinesDatetime);
                }
                else
                {
                    var macdClosinesDatetime = new MACDClosesDateTime
                    {
                        Close = values.Select(x => x.Close).ToList()[i], // or some other value to indicate not enough data
                        Datetime = values.Select(x => x.Datetime).ToList()[i] // keep the original date
                    };
                    //ema.Add((values[i] * multiplier) + (ema[i - 1] * (1 - multiplier)));
                    ema.Add(macdClosinesDatetime);
                }
            }

            return ema;
        }
    }
}
