let objContext = new Object();
//let finnhubKey = 'cjbc5i9r01qn9c1l9ncgcjbc5i9r01qn9c1l9nd0';
//let yfapiKey = 'dJpHrhx6GG9aLvvqoSL0l8QliXlxQbjt55I0YLPY';
//let polygonToken = 'nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD';
//let baseURL = 'https://finnhub.io/api/v1';
////let baseURL2 = 'https://yfapi.net';
//let baseURL3 = 'https://api.polygon.io';

//let results = new Object();
let groupDaily = new Object();
let fromDate;
let toDate;
let fromMonth; 
let fromDay; 
let fromYear; 
let toMonth;
let toDay;
let toYear;
let ticker;
let quote;
let description;

let USDollar = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
});
let number = new Intl.NumberFormat('en-US');
let min = 0;
let addURL = '';
//let finnhub = {
//    Quote: '/quote?symbol={symbol}',
//    Metrics: '/stock/metric?symbol={symbol}&metric=all',
//    IPOCalendar: '/calendar/ipo?from=2023-01-01&to=2023-04-30',
//    SymbolLookup: '/search?q=apple',
//    History: '/stock/candle?symbol={symbol}&resolution=1&from={from}&to={to}',
//    Profile: '/stock/profile2?symbol={AAPL}',
//    News: '/company-news?symbol={symbol}&from={fromDate}&to={toDate}',
//    // {
//    //"category": "technology",
//    //"datetime": 1596589501,
//    //"headline": "Square surges after reporting 64% jump in revenue, more customers using Cash App",
//    //"id": 5085164,
//    //"image": "https://image.cnbcfm.com/api/v1/image/105569283-1542050972462rts25mct.jpg?v=1542051069",
//    //"related": "",
//    //"source": "CNBC",
//    //"summary": "Shares of Square soared on Tuesday evening after posting better-than-expected quarterly results and strong growth in its consumer payments app.",
//    //"url": "https://www.cnbc.com/2020/08/04/square-sq-earnings-q2-2020.html"
//    //}
//    //News: '/news?category=general',
//    Earnings: '/calendar/earnings?from=2023-03-01&to=2023-03-10'
//};
//let yfapi = {
//    Quote: '/v6/finance/quote',
//    SymbolLookup: '/v6/finance/autocomplete',
//    MarketSummary: '/v6/finance/quote/marketSummary?lang=en&region=US',
//    Trending: '/v1/finance/trending/US',
//    History: '/v8/finance/spark?interval=1d&range=1mo&symbols=aapl',
//    Gainers: '/ws/screeners/v1/finance/screener/predefined/saved?count=25&scrIds=day_gainers'

//};

//let polygon = {
//    GroupDaily: '/v2/aggs/grouped/locale/us/market/stocks/{2023-01-09}?adjusted=true&include_otc=false&apiKey=nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD',
//    GainersLosers: '/v2/snapshot/locale/us/markets/stocks/gainers?include_otc=false&apiKey=nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD',
//    History: '/v2/aggs/ticker/{symbol}/range/{multiplier}/{timespan}/{from}/{to}?adjusted=true&sort=asc&limit=120&apiKey=nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD',
//    //Quote: '/v3/reference/tickers?ticker={AAPL}&active=true&apiKey=nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD',
//    Search: '/v3/reference/tickers?search={apple}&active=true&apiKey=nZ6BWDvn_sGspFnsHKfdGBOpGnTI43RD',
//    //Quote: '/v2/snapshot/locale/us/markets/stocks/tickers/{stocksTicker}'
//};

(function (objContext) {   
    /*"use strict";*/
    let baseUrl = window.location.protocol + '//' + window.location.host + '/';
    $(document).ready(function () {
        setTimeout(function () { objContext.init(); }, 100);
        //alert (location.href)
    });

    objContext.init = function () {     
        $('#datepickerFrom').datepicker({
            uiLibrary: 'bootstrap5'
        });
        $('#datepickerTo').datepicker({
            uiLibrary: 'bootstrap5'
        });
        setTimeout(function () {
            $(".overlay").hide();            
        }, 1000);
        $("#PeriodType").on("change", function () {///alert(this.value)
            $('#Period').empty();
            $('#FrequencyType').empty();
            $("#Frequency").empty();
            switch (this.value) {
                case "day":
                    //- valid values are 1, 2, 3, 4, 5, 10
                    // day - valid value is minute
                    addOption($('#Period'), "1", "1");
                    addOption($('#Period'), "2", "2");
                    addOption($('#Period'), "3", "3");
                    addOption($('#Period'), "4", "4");
                    addOption($('#Period'), "5", "5");
                    addOption($('#Period'), "10", "10");
                    addOption($("#FrequencyType"), "minute", "minute");
                    break;
                case "month":
                    //- valid values are 1, 2, 3, 6
                    // month - valid values are daily, weekly
                    addOption($('#Period'), "1", "1");
                    addOption($('#Period'), "2", "2");
                    addOption($('#Period'), "3", "3");
                    addOption($('#Period'), "6", "6");
                    addOption($("#FrequencyType"), "daily", "daily");
                    addOption($("#FrequencyType"), "weekly", "weekly");
                    break;
                case "year":
                    //- valid values are 1, 2, 3, 5, 10, 15, 20
                    // year - valid values are daily, weekly, monthly
                    addOption($('#Period'), "1", "1");
                    addOption($('#Period'), "2", "2");
                    addOption($('#Period'), "3", "3");
                    addOption($('#Period'), "5", "5");
                    addOption($('#Period'), "10", "10");
                    addOption($('#Period'), "15", "15");
                    addOption($('#Period'), "20", "20");
                    addOption($("#FrequencyType"), "daily", "daily");
                    addOption($("#FrequencyType"), "weekly", "weekly");
                    addOption($("#FrequencyType"), "monthly", "monthly");
                    break;
                case "ytd":
                    //- valid values are 1
                    // ytd - valid values are daily, weekly
                    addOption($('#Period'), "1", "1");
                    addOption($("#FrequencyType"), "daily", "daily");
                    addOption($("#FrequencyType"), "weekly", "weekly");
                    break;
                default:
                    break;
            }
            addFrequency($("#FrequencyType").val());
        });
        $("#FrequencyType").on("change", function () {
            $("#Frequency").empty();
            //• minute - valid values are 1, 5, 10, 15, 30
            //• daily - valid value is 1
            //• weekly - valid value is 1
            //• monthly - valid value is 1
            addFrequency(this.value);

        });
        $("#btnAddToList").on("click", function () {
            if (location.href.toString().toLowerCase().indexOf('crypto') > -1) {
                addURL = 'Api/CoinBaseAPI/AddToCryptoTracker/';
            }
            else {
                addURL = 'Api/TraderGoAPI/AddToStockTracker/';
            }
            //alert(quote + "/" + description)
            $.ajax({
                url: addURL + quote + "/" + description,
                //context: document.body
            }).done(function (data) {
                if (data.error) {
                    alert("Add failed try again.");
                    $("#btnAddToList").removeAttr("disabled");
                    $("#btnAddToList").removeAttr("title");
                } else {
                    $("#btnAddToList").attr("disabled", "disabled");
                    $("#btnAddToList").attr("title", quote + " is already in the list");
                    $("#btnAddToList").attr("alt", quote + " is already in the list");
                }
            });
        });
    };


    function addOption(item, text, value) {
        $(item).append($('<option>',
            {
                value: value,
                text: text
            }));
    }
    function addFrequency(frequencyType) {
        switch (frequencyType) {
            case "minute":
                addOption($("#Frequency"), "1", "1");
                addOption($("#Frequency"), "5", "5");
                addOption($("#Frequency"), "10", "10");
                addOption($("#Frequency"), "15", "15");
                addOption($("#Frequency"), "30", "30");
                break;
            case "daily":
                addOption($("#Frequency"), "1", "1");
                break;
            case "weekly":
                addOption($("#Frequency"), "1", "1");
                break;
            case "monthly":
                addOption($("#Frequency"), "1", "1");
                break;
            default:
                break;
        }
    }

    $("#searchHistory").on("click", function () {
        objContext.GetHistoryResults();
    })

    objContext.GetHistoryResults = function () {
        let url = '';
        let openChange = new Array();
        let closeChange = new Array();
        let openPercent = new Array();
        let closePercent = new Array();
        let highChange = new Array();
        let highPercent = new Array();
        let lowChange = new Array();
        let lowPercent = new Array();
        let closePrice = new Array();
        openChange.push(0);
        closeChange.push(0);
        openPercent.push(0);
        closePercent.push(0);
        highChange.push(0);
        highPercent.push(0);
        lowChange.push(0);
        lowPercent.push(0);
        if ($("#PeriodType").val() == "") {
            $("#historyMess").html("Period Type Must be Selected");
            return;
        }
        if ($("#PeriodType").val() != "day") {
            if ($("#startDateHistory").val() == "") {
                $("#historyMess").html("Start Date Must be Selected");
                return;
            }
            if ($("#endDateHistory").val() == "") {
                $("#historyMess").html("End Date Must be Selected");
                return;
            }
            url = "Api/TraderGoAPI/GetPriceHistory/" + quote + "/" + $("#PeriodType").val() + "/" + $("#Period").val() + "/" + $("#FrequencyType").val() + "/" + $("#Frequency").val() + "/" + $("#startDateHistory").val() + "/" + $("#endDateHistory").val() + "";
        }
        else {
            url = "Api/TraderGoAPI/GetPriceHistory/" + quote + "/" + $("#PeriodType").val() + "/" + $("#Period").val() + "/" + $("#FrequencyType").val() + "/" + $("#Frequency").val() + "";
        }
        //alert(url);
        $("#historyMess").html("");
        $.ajax({
            url: url,
            context: document.body
        }).done(function (data) {
            //alert(JSON.stringify(data));
            $("#historyResultsContent").empty();
            //$("#historyResultsContent").html(JSON.stringify(data))
            //let length = data.candles.length;
            ////alert(length)
            //let tr0 = $("<div class='row'></div>");
            //tr0.append("<div class='col text-center'><b>" + description + "</b>: " + quote + "<b> Current Price</b>: " + currentPrice + "<b> Close Price</b>: " + currentClosePrice + "<br><b>RSI</b>: " + data.RSI.toFixed(4) + " <b>SMA</b>: " + data.SMA.toFixed(4) + " <b>EMA</b>: " + data.EMA.toFixed(4) + "<br>An RSI reading of 70 or above indicates an overbought situation.<br>A reading of 30 or below indicates an oversold condition.</div>");
            //$("#historyResultsContent").append(tr0);
            //let tr1 = $("<div class='row'></div>");
            //tr1.append("<div class='col text-center'><b>Number of Days</b>: (" + length + ")</div>");
            //$("#historyResultsContent").append(tr1);
            for (var item in data.candles) {
                closePrice.push(data.candles[item].close)
                if (item > 0) {


                    if (data.candles[item].open >= data.candles[(item - 1)].open) {
                        openChange.push((data.candles[(item)].open - data.candles[item - 1].open).toFixed(4));
                        openPercent.push((data.candles[item].open / data.candles[(item - 1)].open).toFixed(4));
                    } else {
                        openChange.push(-(data.candles[(item - 1)].open - data.candles[item].open).toFixed(4));
                        openPercent.push(-(data.candles[(item - 1)].open / data.candles[item].open).toFixed(4));
                        //openChange[item] = -Math.abs(openChange[item]);
                    }


                    if (data.candles[item].close > data.candles[(item - 1)].close) {
                        closeChange.push((data.candles[(item)].close - data.candles[item - 1].close).toFixed(4));
                        closePercent.push((data.candles[item].close / data.candles[(item - 1)].close).toFixed(4));
                    } else {
                        closeChange.push(-(data.candles[(item - 1)].close - data.candles[item].close).toFixed(4));
                        closePercent.push(-(data.candles[(item - 1)].close / data.candles[item].close).toFixed(4));
                        //closeChange[item] = -closeChange[item];
                    }

                    if (data.candles[item].high > data.candles[(item - 1)].high) {
                        highChange.push((data.candles[(item)].high - data.candles[item - 1].high).toFixed(4));
                        highPercent.push((data.candles[item].high / data.candles[(item - 1)].high).toFixed(4));
                    } else {
                        highChange.push(-(data.candles[(item - 1)].high - data.candles[item].high).toFixed(4));
                        highPercent.push(-(data.candles[(item - 1)].high / data.candles[item].high).toFixed(4));
                        //highChange[item] = -highChange[item];
                    }

                    if (data.candles[item].low > data.candles[(item - 1)].low) {
                        lowChange.push((data.candles[(item)].low - data.candles[item - 1].low).toFixed(4));
                        lowPercent.push((data.candles[item].low / data.candles[(item - 1)].low).toFixed(4));
                    } else {
                        lowChange.push(-(data.candles[(item - 1)].low - data.candles[item].low).toFixed(4));
                        lowPercent.push(-(data.candles[(item - 1)].low / data.candles[item].low).toFixed(4));
                        //lowChange[item] = -lowChange[item];
                    }
                }

                let tr1 = $("<div class='row'></div>");
                tr1.append("<div class='col'><b>open:</b> " + data.candles[item].open + "</div>");
                tr1.append("<div class='col'><b>close:</b> " + data.candles[item].close + "</div>");
                tr1.append("<div class='col'><b>high:</b> " + data.candles[item].high + "</div>");
                $("#historyResultsContent").append(tr1);
                let tr2 = $("<div class='row'></div>");
                tr2.append("<div class='col'><b>low:</b> " + data.candles[item].low + "</div>");
                tr2.append("<div class='col'><b>volume:</b> " + data.candles[item].volume + "</div>");
                tr2.append("<div class='col'><b>date:</b> " + new Date(data.candles[item].datetime).toLocaleString() + "</div>");
                $("#historyResultsContent").append(tr2);
                let tr3 = $("<div class='row'></div>");
                tr3.append("<div class='col'><b>open chg $:</b> " + openChange[item] + "</div>");
                tr3.append("<div class='col'><b>close chg $:</b> " + closeChange[item] + "</div>");
                tr3.append("<div class='col'><b>high chg $:</b> " + highChange[item] + "</div>");
                tr3.append("<div class='col'><b>low chg $:</b> " + lowChange[item] + "</div>");
                $("#historyResultsContent").append(tr3);
                let tr4 = $("<div class='row'></div>");
                tr4.append("<div class='col'><b>open chg %:</b> " + openPercent[item] + "</div>");
                tr4.append("<div class='col'><b>close chg %:</b> " + closePercent[item] + "</div>");
                tr4.append("<div class='col'><b>high chg %:</b> " + highPercent[item] + "</div>");
                tr4.append("<div class='col'><b>low chg %:</b> " + lowPercent[item] + "</div>");
                $("#historyResultsContent").append(tr4);
                $("#historyResultsContent").append("<hr>");
            }
        });
        let myModal = new bootstrap.Modal(document.getElementById('historyResults'))
        myModal.show();
    }
    objContext.GetQuote = function (value, name) {//alert(value)
        //this.value = value.toUpperCase();
        objContext.SetDates();
        setTimeout(function () {
            $("#searchResults").hide();
            objContext.Quote(value, name);
            objContext.GetProfile(value);
            objContext.GetHistory(value);
            objContext.GetCompanyNews(value);
        }, 500);
    }
    objContext.Quote = function (value, name) {
        $(".overlay").show();
        quote = value;
        description = ($.trim(name) == "") ? '' : name;
        //alert(value)
        //alert($('#Search').val())
        //let value = name.value;        
        if ($.trim(value) == "" && $.trim($('#Search').val()) == "") {
            alert("Search field cannot be left blank.");
            return;
        } else
            if ($.trim(value) == "") {
                value = $.trim($('#Search').val()).toUpperCase();
                quote = value;
            }
        $.ajax({
            url: "Api/TraderGoAPI/GetQuote/" + value,
            context: document.body
        }).catch(function (e) { $('#infoWindow').hide(500); alert('error'); }).done(function (data) {
            //alert(JSON.stringify(data));
            //alert(JSON.stringify(data[value]));
            //$(this).addClass("done");
            $("#stockBody").empty();
            //$("#stockBody").append(JSON.stringify(data));
            try {
                currentPrice = data.results[value].quote.lastPrice.toFixed(4);
                currentClosePrice = data.results[value].quote.closePrice.toFixed(4);
                description = data.results[value].reference.description;
                //$("#stockBody").append("<div class='row'><div class='col text-center'><b>" + data[value].reference.description + "</b> :" + value + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Quote</b></div><div class='col'><b>Fundamentals</b></div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end text-success'><b>Last Price</b> :" + data.results[value].quote.lastPrice.toFixed(4) + "</div><div class='col'><b>Div Pay</b> :" + data.results[value].fundamental.divPayAmount.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Close Price</b> :" + data.results[value].quote.closePrice.toFixed(4) + "</div><div class='col'><b>Div Amount</b> :" + data.results[value].fundamental.divAmount.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>High Price</b> :" + data.results[value].quote.highPrice.toFixed(4) + "</div><div class='col'><b>Div Freq</b> :" + data.results[value].fundamental.divFreq.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Low Price</b> :" + data.results[value].quote.lowPrice.toFixed(4) + "</div><div class='col'><b>Div Yield</b> :" + data.results[value].fundamental.divYield.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Change $</b> :" + data.results[value].quote.markChange.toFixed(4) + "</div><div class='col'><b>EPS</b> :" + data.results[value].fundamental.eps.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Change %</b> :" + data.results[value].quote.markPercentChange.toFixed(4) + "</div><div class='col'><b>P/E Ratio</b> :" + data.results[value].fundamental.peRatio.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Ask Price</b> :" + data.results[value].quote.askPrice.toFixed(4) + "</div><div class='col'><b>Last Earnings Date</b> :" + new Date(data.results[value].fundamental.lastEarningsDate).toDateString() + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>Bid Price</b> :" + data.results[value].quote.bidPrice.toFixed(4) + "</div><div class='col'><b>Total Volume</b> :" + data.results[value].quote.totalVolume + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>52 Wk High</b> :" + data.results[value].quote['52WeekHigh'].toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>52 Wk Low</b> :" + data.results[value].quote['52WeekLow'].toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>RSI</b> :" + data.RSI.toFixed(4) + " (14)</div><div class='col'>&nbsp;</div></div>");
/*                $("#stockBody").append("<div class='row'><div class='col border-end'><b>RSI One Hour</b> :" + parseFloat(data.RSIOneHour).toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");*/
                //$("#stockBody").append("<div class='row'><div class='col border-end'>&nbsp;</div><div class='col'><b>SMA (26)</b> :" + data.SMA26.toFixed(4) + "</div></div>");
                //$("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (12)</b> :" + data.SMA12.toFixed(4) + "</div><div class='col'><b>SMA (9)</b> :" + data.SMA9.toFixed(4) + "</div></div>");
                //$("#stockBody").append("<div class='row'><div class='col border-end'>&nbsp;</div><div class='col'><b>EMA (26)</b> :" + data.EMA26.toFixed(4) + "</div></div>");
                //$("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (12)</b> :" + data.EMA12.toFixed(4) + "</div><div class='col'><b>EMA (9)</b> :" + data.EMA9.toFixed(4) + "</div></div>");

                $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (3)</b> :" + data.SMA3.toFixed(4) + "</div><div class='col border-end'><b>SMA (10)</b> :" + data.SMA10.toFixed(4) + "</div><div class='col'><b>SMA (16)</b> :" + data.SMA16.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (3)</b> :" + data.EMA3.toFixed(4) + "</div><div class='col border-end'><b>EMA (10)</b> :" + data.EMA10.toFixed(4) + "</div><div class='col'><b>EMA (16)</b> :" + data.EMA16.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (5)</b> :" + data.SMA5.toFixed(4) + "</div><div class='col border-end'><b>SMA (8)</b> :" + data.SMA8.toFixed(4) + "</div><div class='col'><b>SMA (13)</b> :" + data.SMA13.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (5)</b> :" + data.EMA5.toFixed(4) + "</div><div class='col border-end'><b>EMA (8)</b> :" + data.EMA8.toFixed(4) + "</div><div class='col'><b>EMA (13)</b> :" + data.EMA13.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (26)</b> :" + data.SMA26.toFixed(4) + "</div><div class='col border-end'><b>SMA (12)</b> :" + data.SMA12.toFixed(4) + "</div><div class='col'><b>SMA (9)</b> :" + data.SMA9.toFixed(4) + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (26)</b> :" + data.EMA26.toFixed(4) + "</div><div class='col border-end'><b>EMA (12)</b> :" + data.EMA12.toFixed(4) + "</div><div class='col'><b>EMA (9)</b> :" + data.EMA9.toFixed(4) + "</div></div>");

                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD</b> :" + data.MACD + "</div><div class='col'><b>MACD Signal</b> :" + data.MACDSignal + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram</b> :" + data.MACDHistogram + "</div><div class='col'>&nbsp;</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD 2</b> :" + data.MACD2 + "</div><div class='col'><b>MACD Signal 2</b> :" + data.MACDSignal2 + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram 2</b> :" + data.MACDHistogram2 + "</div><div class='col'>&nbsp;</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD 3</b> :" + data.MACD3 + "</div><div class='col'><b>MACD Signal 3</b> :" + data.MACDSignal3 + "</div></div>");
                $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram 2</b> :" + data.MACDHistogram3 + "</div><div class='col'>&nbsp;</div></div>");

                //$("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD DD 2</b> :" + data.MACDDD2.toFixed(4) + "</div><div class='col'><b>MACD Signal DD 2</b> :" + data.MACDSignalDD2.toFixed(4) + "</div></div>");
                //$("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram DD 2</b> :" + data.MACDHistogramDD2.toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");

                $("#stockBody").append("<div class='row'><div class='col' style='line-height:15px;'>An RSI reading of 70 or above indicates an overbought situation.<br> A reading of 30 or below indicates an oversold condition.</div></div>");
                $("#stockBody").append("<div class='row'><div class='col' style='line-height:15px;'>When the MACD crosses above its signal line, it's a technical signal to buy. When it crosses below, it's a signal to sell.</div></div>");
                $(".overlay").hide();
                $.ajax({
                    url: "Api/TraderGoAPI/StockTracker/" + value,
                    //context: document.body
                }).done(function (data) {
                    if (data.error) {
                        $("#btnAddToList").removeAttr("disabled");
                        $("#btnAddToList").removeAttr("title");
                    } else {
                        $("#btnAddToList").attr("disabled", "disabled");
                        $("#btnAddToList").attr("title", value + " is already in the list");
                        $("#btnAddToList").attr("alt", value + " is already in the list");
                    }
                }).catch(function (e) { $('#infoWindow').hide(500) });
            } catch (e) {
                $(".overlay").hide();
                alert(e.name + ": " + e.stack + ": " + e.message);
            }

        });
        //let myModal = new bootstrap.Modal(document.getElementById('stockResults'))
        //myModal.show();
        //objSearch.ShowHideResults(true);
        $('#infoWindow').show(500)
        objContext.StockInfo(1)
    }
    objContext.Quote2 = function (value, name) {
        $(".overlay").show();
        quote = value;
        description = name;
        $("#infoTitle").html(value + ': ' + name);
        //alert(value)
        //alert($('#Search').val())
        //let value = name.value;        
        if ($.trim(value) == "" && $.trim($('#Search').val()) == "") {
            alert("Search field cannot be left blank.");
            return;
        } else
            if ($.trim(value) == "") {
                value = $.trim($('#Search').val()).toUpperCase();
                quote = value;
            }
        $.ajax({
            url: "Api/CoinBaseAPI/GetQuote/" + value,
            context: document.body
        }).catch(function (e) { $('#infoWindow').hide(500); alert('error'); }).done(function (data) {
            //alert(JSON.stringify(data));
            //alert(JSON.stringify(data));
            //$(this).addClass("done");
            $("#stockBody").empty();
            //$("#stockBody").append(JSON.stringify(data));
            currentPrice = data.lastPrice//.toFixed(4);
            //currentClosePrice = data.closePrice.toFixed(4);
            //description = data.Name;
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>Quote</b></div><div class='col'><b>Fundamentals</b></div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end text-success'><b>Last Price</b> :" + parseFloat(data.lastPrice).toFixed(4) + "</div><div class='col'><b>&nbsp;</b> &nbsp;</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>RSI</b> :" + parseFloat(data.RSI).toFixed(4) + " (14)</div><div class='col'>&nbsp;</div></div>");
/*            $("#stockBody").append("<div class='row'><div class='col border-end'><b>RSI One Hour</b> :" + parseFloat(data.RSIOneHour).toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");*/
            //$("#stockBody").append("<div class='row'><div class='col border-end'>&nbsp;</div><div class='col'><b>SMA (26)</b> :" + parseFloat(data.SMA26).toFixed(4) + "</div></div>");
            //$("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (12)</b> :" + parseFloat(data.SMA12).toFixed(4) + "</div><div class='col'><b>SMA (9)</b> :" + parseFloat(data.SMA9).toFixed(4) + "</div></div>");
            //$("#stockBody").append("<div class='row'><div class='col border-end'>&nbsp;</div><div class='col'><b>EMA (26)</b> :" + parseFloat(data.EMA26).toFixed(4) + "</div></div>");
            //$("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (12)</b> :" + parseFloat(data.EMA12).toFixed(4) + "</div><div class='col'><b>EMA (9)</b> :" + parseFloat(data.EMA9).toFixed(4) + "</div></div>");

            $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (3)</b> :" + data.SMA3.toFixed(4) + "</div><div class='col border-end'><b>SMA (10)</b> :" + data.SMA10.toFixed(4) + "</div><div class='col'><b>SMA (16)</b> :" + data.SMA16.toFixed(4) + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (3)</b> :" + data.EMA3.toFixed(4) + "</div><div class='col border-end'><b>EMA (10)</b> :" + data.EMA10.toFixed(4) + "</div><div class='col'><b>EMA (16)</b> :" + data.EMA16.toFixed(4) + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (5)</b> :" + data.SMA5.toFixed(4) + "</div><div class='col border-end'><b>SMA (8)</b> :" + data.SMA8.toFixed(4) + "</div><div class='col'><b>SMA (13)</b> :" + data.SMA13.toFixed(4) + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (5)</b> :" + data.EMA5.toFixed(4) + "</div><div class='col border-end'><b>EMA (8)</b> :" + data.EMA8.toFixed(4) + "</div><div class='col'><b>EMA (13)</b> :" + data.EMA13.toFixed(4) + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>SMA (26)</b> :" + data.SMA26.toFixed(4) + "</div><div class='col border-end'><b>SMA (12)</b> :" + data.SMA12.toFixed(4) + "</div><div class='col'><b>SMA (9)</b> :" + data.SMA9.toFixed(4) + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>EMA (26)</b> :" + data.EMA26.toFixed(4) + "</div><div class='col border-end'><b>EMA (12)</b> :" + data.EMA12.toFixed(4) + "</div><div class='col'><b>EMA (9)</b> :" + data.EMA9.toFixed(4) + "</div></div>");

            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD</b> :" + data.MACD + "</div><div class='col'><b>MACD Signal</b> :" + data.MACDSignal + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram</b> :" + data.MACDHistogram + "</div><div class='col'>&nbsp;</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD 2</b> :" + data.MACD2 + "</div><div class='col'><b>MACD Signal 2</b> :" + data.MACDSignal2 + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram 2</b> :" + data.MACDHistogram2 + "</div><div class='col'>&nbsp;</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD 3</b> :" + data.MACD3 + "</div><div class='col'><b>MACD Signal 3</b> :" + data.MACDSignal3 + "</div></div>");
            $("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram 2</b> :" + data.MACDHistogram3 + "</div><div class='col'>&nbsp;</div></div>");

            //$("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD DD 2</b> :" + data.MACDDD2.toFixed(4) + "</div><div class='col'><b>MACD Signal DD 2</b> :" + data.MACDSignalDD2.toFixed(4) + "</div></div>");
            //$("#stockBody").append("<div class='row'><div class='col border-end'><b>MACD Histogram DD 2</b> :" + data.MACDHistogramDD2.toFixed(4) + "</div><div class='col'>&nbsp;</div></div>");

            $("#stockBody").append("<div class='row'><div class='col' style='line-height:15px;'>An RSI reading of 70 or above indicates an overbought situation.<br> A reading of 30 or below indicates an oversold condition.</div></div>");
            $("#stockBody").append("<div class='row'><div class='col' style='line-height:15px;'>When the MACD crosses above its signal line, it's a technical signal to buy. When it crosses below, it's a signal to sell.</div></div>");
            $(".overlay").hide();
            $.ajax({
                url: "Api/CoinBaseAPI/CryptoTracker/" + value,
                //context: document.body
            }).done(function (data) {
                if (data.error) {
                    $("#btnAddToList").removeAttr("disabled");
                    $("#btnAddToList").removeAttr("title");
                } else {
                    $("#btnAddToList").attr("disabled", "disabled");
                    $("#btnAddToList").attr("title", value + " is already in the list");
                    $("#btnAddToList").attr("alt", value + " is already in the list");
                }
            }).catch(function (e) { $('#infoWindow').hide(500) });
        });
        //let myModal = new bootstrap.Modal(document.getElementById('stockResults'))
        //myModal.show();
        //objSearch.ShowHideResults(true);
        $('#infoWindow').show(500)
        objContext.StockInfo(1)
    }
    objContext.GetProfile = function (value) {
        let profile;
        $("#infoTitle").empty();
        $("#searchResults").empty();
        $("#infoWindowTextCenter").empty();
        fetch(baseUrl + '../../api/TraderGoAPI/GetProfile/' + value)
            .then(response => {
                if (!response.ok) {
                    console.error("CompanyNewsyResults: Network error while retriving response. ")
                } else {
                    return response.text();
                }
            }).then(data => {
                profile = JSON.parse(data)
            })
            .then(() => {
                //alert(JSON.stringify(profile))

                let capitalized;
                for (var key in profile.results) {
                    //alert(key); keyValue = "";
                    if ($.trim(key) == 'branding') {
                    } else if ($.trim(key) == 'address') {
                        //do nothing
                    } else {//alert(key)
                        output = profile.results[key]
                        keyValue = key.replaceAll('_', ' ')
                        capitalized =
                            keyValue.charAt(0).toUpperCase()
                            + keyValue.slice(1)
                        if ($.trim(key) == 'name') {
                            $("#infoTitle").html(output + ": " + value);
                        }
                        $("#infoWindowTextCenter").append("<b>" + capitalized + ":</b> " + output + "<br>")
                    }
                }
            }).catch(function (error) {
                console.log("error" + error);
                $(".overlay").hide();
            });
    }

    objContext.GetHistory = function (value) {//alert(url)
        objChart.InitData();
        let openArray = []; //let open
        fetch(baseUrl + "../../api/TraderGoAPI/GetHistory/" + value + "/" + fromYear + "-" + fromMonth + "-" + fromDay + "/" + toYear + "-" + toMonth + "-" + toDay)
            .then(response => {
                if (!response.ok) {
                    console.error("HistoryResults: Network error while retriving response. ");// + JSON.stringify(response.text()))
                } else {
                    return response.text();
                }
            }).then(data => {
                //alert(JSON.stringify(data));
                ticker = JSON.parse(data);
                //alert(JSON.stringify(ticker) + " get history");
                $("#searchResults").empty();
                $("#infoWindowTextRight").empty();

                for (var items in ticker.results) {
                    openArray = []
                    openArray.push(new Date(ticker.results[items].t).getDate())
                    openArray.push(ticker.results[items].o)
                    //datasets.Open.data.length = 0
                    if (min == 0 || min > ticker.results[items].o) {
                        min = ticker.results[items].o;
                    }
                    datasets.Open.data.push(openArray)

                    for (var key in ticker.results[items]) {
                        let preClosePrice = 0;
                        let change;
                        let changePercent;
                        //alert(key + " Key")
                        switch (key) {
                            case "c":
                                output = USDollar.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'Close'
                                try {
                                    //alert(items)
                                    //change = USDollar.format((parseFloat(parseFloat(ticker.results[items][key])) - parseFloat(parseFloat(ticker.results[items - 1][key]))));
                                    change = (parseFloat(parseFloat(ticker.results[items][key])) - parseFloat(parseFloat(ticker.results[items - 1][key])));
                                    preClosePrice = USDollar.format(parseFloat(ticker.results[items - 1][key]));
                                    changePercent = ((Math.abs(parseFloat(change)) / parseFloat(ticker.results[items - 1][key])) * 100);
                                    change = USDollar.format(change);
                                    changePercent = Math.round(changePercent, 4);
                                    //datasets.Close.data[0].push()
                                    //alert(parseFloat(ticker.results[items - 1][key]))
                                    //alert(parseFloat(change))
                                } catch (e) {

                                }
                                break;
                            case "h":
                                output = USDollar.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'High'
                                break;
                            case "l":
                                output = USDollar.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'Low'
                                break;
                            case "o":
                                output = USDollar.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'Open'
                                break;
                            case "v":
                                output = number.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'Volume'
                                break;
                            case "vw":
                                output = USDollar.format(parseFloat(ticker.results[items][key]))
                                keyValue = 'Weighted average price'
                                break;
                            case "t":
                                output = new Date(ticker.results[items][key]).toUTCString()
                                keyValue = 'Date'
                                break;
                            default:
                                keyValue = '';
                                break;
                        }
                        if ($.trim(keyValue) != '')
                            $("#infoWindowTextRight").append("<b>" + keyValue + ":</b> " + output + "<br>")
                        if (preClosePrice != 0) {
                            let color = ''
                            //if (preClosePrice > output) {
                            if (output > preClosePrice) {
                                color = 'green'
                            } else if (preClosePrice == output) {
                                color = 'black'
                            }
                            else {
                                color = 'red'
                            }
                            //$("#infoWindowTextRight").append("<b>Previous Close Price:</b> <span style='color: " + color + ";'>" + preClosePrice + "</span><br>");
                            $("#infoWindowTextRight").append("<b>Previous Close Price:</b> " + preClosePrice + "<br>");
                            $("#infoWindowTextRight").append("<b>Change:</b> <span style='color: " + color + ";'>" + change + "</span><br>");
                            $("#infoWindowTextRight").append("<b>Change:</b> <span style='color: " + color + ";'>" + changePercent + "%</span><br>");
                        }


                    }

                    $("#infoWindowTextRight").append("<hr>");
                }
                //alert(JSON.stringify(datasets))
            }).catch(function (error) {
                console.log("error" + error);
                $(".overlay").hide();
            });
    }

    objContext.GetCompanyNews = function (value) {
        //alert(url + value)
        fetch(baseUrl + "../../api/TraderGoAPI/GetCompanyNews/" + value + "/" + fromYear + "-" + fromMonth + "-" + fromDay + "/" + toYear + "-" + toMonth + "-" + toDay)
            .then(response => {
                if (!response.ok) {
                    console.error("CompanyNewsyResults: Network error while retriving response. ")
                } else {
                    return response.text();
                }
            }).then(data => {
                //alert(JSON.stringify(data));
                $("#infoNews").empty();
                data = JSON.parse(data);
                //alert(JSON.stringify(data))
                for (var item in data) {
                    //alert(JSON.stringify(item))
                    $("#infoNews").append("<h5 style='white-space:nowrap;'><a href='" + data[item].url + "' target='_blank'>" + data[item].headline + "</a></h5>")
                    $("#infoNews").append("<p><img src='" + data[item].image + "' height='50' />&nbsp;&nbsp;<strong>" + new Date(data[item].datetime * 1000).toLocaleString() + "</strong></p>")
                    $("#infoNews").append("<p style='text-align: justify;border: 1px solid #cacaca;padding: 5px;border - radius: 3px;'>" + data[item].summary + "</p>")
                }
                //alert(JSON.stringify(ticker) + " get history");
            }).catch(function (error) {
                console.log("error" + error);
                $(".overlay").hide();
            });
    }

    objContext.GetGroupDaily = async function (url) {
        $(".overlay").show();
        let options = {
            method: "GET", // *GET, POST, PUT, DELETE, etc.
            mode: "cors", // no-cors, *cors, same-origin
            redirect: 'follow',
            //cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
            credentials: "same-origin",//"same-origin", // include, *same-origin, omit
            headers: {
                "Accept": "application/json",
                /*"User-Agent": "samanbagheri",*/
                "Accept-Encoding": "gzip, deflate, br",
                "Connection": "keep-alive",
                "Content-Type": "application/json"//,
                //"Authorization": "Bearer " + polygonToken
            },
            //redirect: "follow", // manual, *follow, error
            //referrerPolicy: "no-referrer", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
            //body: JSON.stringify(data), // body data type must match "Content-Type" header
        }
        await fetch(url, options)
            .then(response => {
                if (!response.ok) {
                    alert("GroupDailyResults: Network error while retriving response. ");// + JSON.stringify(response.text()))
                }
                return response.text();
            }).then(data => {
                //alert(JSON.stringify(data));
                groupDaily = JSON.parse(data);
            }).then(() => {
                $("#modalText").empty();
                //alert(JSON.stringify(results));
                $(".model-header").html("<p>Trend Search Results: Found " + groupDaily.queryCount + "</p>")
                //$("#ModalMessage").modal("show")
                //$("#ModalMessage").modal({ backdrop: 'static', keyboard: false }).show(); 
                var myModal = new bootstrap.Modal(document.getElementById('ModalMessage'), {
                    keyboard: false,
                    backdrop: 'static'
                })
                myModal.show();
            }).then(() => {
                $(".overlay").hide();
            }).catch(function (error) {
                console.log("error" + error);
                $(".overlay").hide();
            });
        let count = 0;
        $("#exampleModalLabel").html("Trend Search Results: Found " + count + "")
        let p = $("<div class='row'></div>").append("<div class='col'>Symbol</div> <div class='col'>From Date</div><div class='col'>Start Price</div><div class='col'>To Date</div><div class='col'>End Price</div>");
        $("#modalText").append(p)
        for (var item in groupDaily.results) {
            //await objContext.GetHistory("../../api/TraderGoAPI/GetHistory/" + $.trim(groupDaily.results[item].T) + "/" + fromYear + "-" + fromMonth + "-" + fromDay + "/" + toYear + "-" + toMonth + "-" + toDay);
            await objContext.GetHistory($.trim(groupDaily.results[item].T));
            try {
                let startPrice = USDollar.format(ticker.results[0].c)
                let endPrice = USDollar.format(ticker.results[ticker.results.length - 1].c)
                //alert($("#direction").val())
                if ($("#direction").val() == "Up") {
                    if (endPrice > startPrice) {
                        let p = $("<div class='row'></div>").append("<div class='col'><a style='color:#fff;' href=\"javascript:objContext.GetQuote(\'" + $.trim(groupDaily.results[item].T) + "\',\'\');\">" + groupDaily.results[item].T + "</a></div> <div class='col'>" + fromDate.toLocaleDateString() + "</div><div class='col'>" + startPrice + "</div><div class='col'>" + toDate.toLocaleDateString() + "</div><div class='col'>" + endPrice + "</div>");
                        $("#modalText").append(p)
                        count++;
                        $("#exampleModalLabel").html("Trend Search Results: Found " + count + "")
                    }
                } else {
                    if (endPrice < startPrice) {
                        let p = $("<div class='row'></div>").append("<div class='col'><a style='color:#fff;' href=\"javascript:objContext.GetQuote(\'" + $.trim(groupDaily.results[item].T) + "\',\'\');\">" + groupDaily.results[item].T + "</a></div> <div class='col'>" + fromDate.toLocaleDateString() + "</div><div class='col'>" + startPrice + "</div><div class='col'>" + toDate.toLocaleDateString() + "</div><div class='col'>" + endPrice + "</div>");
                        $("#modalText").append(p)
                        count++;
                        $("#exampleModalLabel").html("Trend Search Results: Found " + count + "")
                    }
                }
            } catch (e) {
                //alert(e)
            }
        }

    }

    objContext.Search = function (name) {
        //alert(name)
        let value = name;
        //alert(value)
        if (value.length >= 3) {
            $("#searchResults").show();
            $.ajax({
                url: "Api/TraderGoAPI/SearchSchwab/" + value,
                context: document.body
            }).done(function (data) {
                //alert(JSON.stringify(data))
                $("#searchResults").empty();
                let result = data;//.instruments;
                for (var item in result) {
                    let row = $("<div class='row'></div>")
                    row.append("<div class='col p-1 overflow-hidden'><a href=\"javascript:objContext.GetQuote('" + result[item].symbol + "','" + result[item].description + "')\">" + result[item].symbol + "</a></div>")
                    row.append("<div class='col-5 p-1 overflow-hidden'>" + result[item].description + "</div>")
                    row.append("<div class='col-5 p-1 overflow-hidden'>" + result[item].exchange + "</div>")
                    row.append("<div class='col p-1 overflow-hidden'>" + result[item].assetType + "</div>")
                    $("#searchResults").append(row)
                }
                //$("#searchResults").html(JSON.stringify(data));
            });
        } else {
            $("#searchResults").hide();
            $("#searchResults").empty();
        }

        //let myModal = new bootstrap.Modal(document.getElementById('stockResults'))
        //myModal.show();
    }

    let offSet = 1;
    objContext.SetDates = function () {
        if ($.trim($("#datepickerFrom").val()) != '' && $.trim($("#datepickerTo").val()) != '') {
            fromDate = new Date($("#datepickerFrom").val());
            toDate = new Date($("#datepickerTo").val());
        } else {
            fromDate = new Date();
            toDate = new Date();
        }

        fromDate.setDate(fromDate.getDate() - offSet);
        toDate.setDate(toDate.getDate() - offSet);
        if (fromDate.getDay() == 0 || fromDate.getDay() == 7)
            offSet -= 1;

        fromDate.setDate(toDate.getDate() - 30);
        fromMonth = ((fromDate.getMonth() + 1).toString().length == 1) ? "0" + (fromDate.getMonth() + 1).toString() : (fromDate.getMonth() + 1).toString();
        fromDay = (fromDate.getDate().toString().length == 1) ? "0" + fromDate.getDate().toString() : fromDate.getDate().toString();
        fromYear = fromDate.getFullYear()
        toMonth = ((toDate.getMonth() + 1).toString().length == 1) ? "0" + (toDate.getMonth() + 1).toString() : (toDate.getMonth() + 1).toString();
        toDay = (toDate.getDate().toString().length == 1) ? "0" + toDate.getDate().toString() : toDate.getDate().toString();
        toYear = toDate.getFullYear()   

    }

    objContext.ShowLoginRegisterRecover = function (item) {
        switch (item) {
            case 1:
                $("#divLogin").show();
                $("#divRegister").hide();
                $("#divRecover").hide();
                break;
            case 2:
                $("#divLogin").hide();
                $("#divRegister").show();
                $("#divRecover").hide();
                break;
            case 3:
                $("#divLogin").hide();
                $("#divRegister").hide();
                $("#divRecover").show();
                break;
        }
    };

    objContext.TogglePassword = function (item) {
        let input = document.getElementById(item);
        if (input.type == 'password') {
            input.type = 'text';
        }
        else {
            input.type = 'password';
        }
    };

    objContext.StockInfo = function (item) {
        switch (item) {
            case 1:
                $("#infoWindowTextLeft").show();
                $("#infoWindowTextCenter").hide();
                $("#infoWindowTextRight").hide();
                $("#infoNews").hide();
                $("#infoWindowChart").hide();
                break;
            case 2:
                $("#infoWindowTextLeft").hide();
                $("#infoWindowTextCenter").show();
                $("#infoWindowTextRight").hide();
                $("#infoNews").hide();
                $("#infoWindowChart").hide();
                break;
            case 3:
                $("#infoWindowTextLeft").hide();
                $("#infoWindowTextCenter").hide();
                $("#infoWindowTextRight").show();
                $("#infoNews").hide();
                $("#infoWindowChart").hide();
                break;
            case 4:
                $("#infoWindowTextLeft").hide();
                $("#infoWindowTextCenter").hide();
                $("#infoWindowTextRight").hide();
                $("#infoNews").show();
                $("#infoWindowChart").hide();
                break;
            case 5:
                $("#infoWindowTextLeft").hide();
                $("#infoWindowTextCenter").hide();
                $("#infoWindowTextRight").hide();
                $("#infoNews").hide();
                $("#infoWindowChart").show();
                objChart.SetChart(min);
                break;
        }
    }
    objContext.DeleteFromStockTracker = function (symbol) {
        if (confirm("Are you sure yo want to delete " + symbol + "?")) {
            $.ajax({
                url: "Api/TraderGoAPI/DeleteFromStockTracker/" + symbol,
                //context: document.body
            }).done(function (data) {
                if (data.error) {
                    alert("Add failed try again.");
                    $("#btnAddToList").removeAttr("disabled");
                    $("#btnAddToList").removeAttr("title");
                } else {
                    window.location.reload();
                }
            });
        }
    }

    objContext.DeleteFromCryptoTracker = function (symbol) {
        if (confirm("Are you sure yo want to delete " + symbol + "?")) {
            $.ajax({
                url: "Api/CoinBaseGoAPI/DeleteFromCryptoTracker/" + symbol,
                //context: document.body
            }).done(function (data) {
                if (data.error) {
                    alert("Add failed try again.");
                    $("#btnAddToList").removeAttr("disabled");
                    $("#btnAddToList").removeAttr("title");
                } else {
                    window.location.reload();
                }
            });
        }
    }

    objContext.PostStockList = function (item,form) {
        let itemName = item.id;
        //alert($("#" + itemName).is(":checked"))
        //alert(form)
        if ($("#" + itemName).is(":checked"))
            $("#" + itemName).val(true);
        else
            $("#" + itemName).val(false);
        $("#" + form).submit();
    }

    objContext.PostCryptoList = function (item, form) {
        let itemName = item.id;
        //alert($("#" + itemName).is(":checked"))
        //alert(form)
        if ($("#" + itemName).is(":checked"))
            $("#" + itemName).val(true);
        else
            $("#" + itemName).val(false);
        $("#" + form).submit();
    }
    objContext.sleep = function (ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}) (objContext)