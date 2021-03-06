# Trady
[![build](https://github.com/lppkarl/trady/workflows/build/badge.svg)](https://github.com/lppkarl/Trady/actions?query=workflow%3A%22build%22)
[![NuGet Pre Release](https://img.shields.io/nuget/v/Trady.Analysis.svg)](https://www.nuget.org/packages/Trady.Analysis/3.2.8)
[![NuGet](https://img.shields.io/nuget/dt/Trady.Analysis.svg)](https://www.nuget.org/packages/Trady.Analysis/3.2.8)
[![license](https://img.shields.io/github/license/lppkarl/Trady.svg)](LICENSE)

Trady is a handy library for computing technical indicators, and targets to be an automated trading system that provides stock data feeding, indicator computing, strategy building and automatic trading. It is built based on .NET Standard 2.0.

## Read Before You Use
This library is a hobby project, and would probably making breaking changes, use with care when in production.

## Currently Available Features
* Stock data feeding (including csv, yahoo finance, quandl, alphavantage, stooq)
* Indicator computing (including SMA, EMA, RSI, MACD, BB, etc.)
* Signal capturing by rules
* Strategy backtesting by buy/sell rule

## v3.2.8 update
* Fixes IsBull, IsBear recognition, added IsDoji extension by @ridicoulous
* Added vwap indicator by @codebeaulieu

## v3.2.7 update
* Added candles transformation by @ridicoulous

## v3.2.2 update
* Fixes CommodityChannelIndex

## v3.2 update
* Added WeightedMovingAverage, HullMovingAverage & KeltnerChannels
* Added AlphaVantage importer (thanks to @irperez)
* Reactivate support for QuandlImporter
* Boost performance on date time transformation (thanks to @pavlexander)
* Boost performance for various indicators (HistoricalHighest/HistoricalLowest/EmaOsc/Macd/ADLine/Obv/ParabolicSar, etc.)
* Update dependencies for importers
* Remove redundant sdCount parameter for Sd operation
* EffeciencyRatio & Kama now accepts nullable decimal as input
* Allow use of simple operation for ParabolicSar
* Renamed simple operation "PcDiff" to "RDiff"

For older version history, please refer to another markdown document [here](release_notes.md)

## Updates from 2.0.x to 3.0.0
Please refer to another markdown document [here](update_from_2.0.x.md)

## Supported Platforms
* .NET Core 2.0 or above
* .NET Framework 4.6.1 or above
* Mono 5.4 or above
* Xamarin.iOS 10.4 or above
* Xamarin.Android 7.5 or above
* Xamarin.Mac 3.8 or above

## Currently Supported Importers
* Csv file
* Yahoo Finance
* Quandl
* Stooq
* AlphaVantage

## Currently Supported Indicators
Please refer to another markdown document [here](supported_indicators.md)

## How To Install
Nuget package is available in modules, please install the package according to the needs

    // For importing
    PM > Install-Package Trady.Importer

    // For computing & backtesting
    PM > Install-Package Trady.Analysis

## How To Use
<a name="Content"></a>
* Tldr
    * [Tldr](#tldr)

* Importing
    * [Import Stock Data](#ImportStockData)
    
* Computing
    * [Transform Stock Data](#TransformStockData)
    * [Compute Indicator](#ComputeIndicators)
    * [Compute Simple Operations on Indicator](#ComputeIndicatorsOperation)
    * [Convert Func to Indicator](#ConvertFunctionToAnalyzable)
    * [Register Func for Global Use](#RegisterFuncForGlobalUse)
    
* Backtesting
    * [Capture Signals by Rules](#CaptureSignalByRules)
    * [Strategy Building and Backtesting](#StrategyBuildingAndBacktesting)
    * [Implement Rule Pattern](#ImplementYourOwnPattern)
    * [Register Rule for Global Use](#RegisterRuleForGlobalUse)

* Advanced
    * [Implement Your Own Importer](#ImplementYourOwnImporter)
    * [Implement Your Own Indicator - Simple Type](#ImplementYourOwnIndicatorSimpleType)
    * [Implement Your Own Indicator - Cummulative Type](#ImplementYourOwnIndicatorCummulativeType)
    * [Implement Your Own Indicator - Moving Average Type](#ImplementYourOwnIndicatorMovingAverageType)

<a name="tldr"></a>
### Tldr
    var importer = new YahooFinanceImporter();
    var candles = await importer.ImportAsync("FB");
    var last = candles.Sma(30).Last();
    Console.WriteLine($"{last.DateTime}, {last.Tick}");
[Back to content](#Content)

<a name="ImportStockData"></a>
### Import stock data
    // From Quandl wiki database
    var importer = new QuandlWikiImporter(apiKey);

    // From Yahoo Finance
    var importer = new YahooFinanceImporter();

    // From Stooq
    var importer = new StooqImporter();

    // From AlphaVantage
    var importer = new AlphaVantageImporter(apiKey);

    // From Google Finance (Temporarily not available now)
    // var importer = new GoogleFinanceImporter();

    // Or from dedicated csv file
    var importer = new CsvImporter("FB.csv");

    // Get stock data from the above importer
    var candles = await importer.ImportAsync("FB");
[Back to content](#Content)

<a name="TransformStockData"></a>
### Transform stock data to specified period before computation
    // Transform the series for computation, downcast is forbidden
    // Supported period: PerSecond, PerMinute, Per15Minutes, Per30Minutes, Hourly, BiHourly, Daily, Weekly, Monthly

    var transformedCandles = candles.Transform<Daily, Weekly>();
[Back to content](#Content)

<a name="ComputeIndicators"></a>
### Compute indicator
    // This library supports computing from tuples or candles, extensions are recommended for computing
    var closes = new List<decimal>{ ... };
    var smaTs = closes.Sma(30);
    var sma = closes.Sma(30)[index];

    // or, traditional call
    var sma = new SimpleMovingAverageByTuple(closes, 30)[index];
    
    // the corresponding version of candle
    var sma = new SimpleMovingAverage(candles, 30)[index];
[Back to content](#Content)

<a name="ComputeIndicatorsOperation"></a>
### Compute simple operation on an indicator
    // Simple operation on indicator is supported, now supports only diff, rDiff, sma, sd
    var closes = new List<decimal>{ ... };
    var smaDiff = closes.Sma(30).Diff(index);   // i-th term - (i-1)-th term
    var smaSma = closes.Sma(30).Sma(10, index); // average(n items)
    var smaRDiff = closes.Sma(30).RDiff(index); // (i-th term - (i-1)-th term) / (i-1)-th term * 100
    var smaSd = closes.Sma(30).Sd(10, 2, index);

    // This also applies to candles
[Back to content](#Content)

<a name="ConvertFunctionToAnalyzable"></a>
### Convert function to indicator
    // Sometimes, we want to utilize the Analyzable infra for some simple indicators but doesn't want to implement a new class, we are adding AsAnalyzable for conversion from Func

    // Before conversion, on the very top of your file, you should add
    using AFunc = System.Func<System.Collections.Generic.IReadOnlyList<Trady.Core.Candle>, int, System.Collections.Generic.IReadOnlyList<decimal>, Trady.Core.Infrastructure.IAnalyzeContext<Trady.Core.Candle>, decimal?>;

    // And use it in your code
    AFunc aFunc = (c, i, p, ctx) => c[i].High - c[i].Low;   // The four parameters: candles, index, parameters, context
    var aFuncInstance = aFunc.AsAnalyzable(candles);
    var a = aFuncInstance[index];

    // You may also combine with simple operation computation
    var aSma = aFuncInstance.Sma(30, index);
[Back to content](#Content)

<a name="RegisterFuncForGlobalUse"></a>
### Register function for global use
    // To use your func globally for analysis, you can register you func by using FuncRegistry.Register method
    // The four parameters is the same as the above section, namely: candles, index, parameters, context
    FuncRegistry.Register("modified_sma", (c, i, p, ctx) => ctx.Get<SimpleMovingAverage>(p[0])[i].Tick);

    // You can use your func globally using the extension
    var lastModifiedSmaValue = candles.Func("modified_sma", 10)[candles.Count() - 1];

    // The library also support register by plain text expression, you can dynamically create an analyzable as follows:
    // Please notice that you must follow the naming convention: c, i, p, ctx when using this approach
    FuncRegistry.Register("dy_sma", "var sma = ctx.Get<SimpleMovingAverage>(10); return sma[i].Tick;");

    // And the use case is similar:
    var lastModifiedSmaValue = candles.Func("modified_sma")[candles.Count() - 1];

<a name="CaptureSignalByRules"></a>
### Capture signals by rules
    // The following shows the number of candles that fulfill both the IsAboveSma(30) & IsAboveSma(10) rule
    var rule = Rule.Create(c => c.IsAboveSma(30))
        .And(c => c.IsAboveSma(10));

    // Use context here for caching indicator results
    using (var ctx = new AnalyzeContext(candles))
    {
        var validObjects = new SimpleRuleExecutor(ctx, rule).Execute();
        Console.WriteLine(validObjects.Count());
    }

<a name="StrategyBuildingAndBacktesting"></a>
### Strategy building & backtesting
    // Import your candles
    var importer = new YahooFinanceImporter();
    var fb = await importer.ImportAsync("FB");
    var aapl = await importer.ImportAsync("AAPL");

    // Build buy rule & sell rule based on various patterns
    var buyRule = Rule.Create(c => c.IsFullStoBullishCross(14, 3, 3))
        .And(c => c.IsMacdOscBullish(12, 26, 9))
        .And(c => c.IsSmaOscBullish(10, 30))
        .And(c => c.IsAccumDistBullish());

    var sellRule = Rule.Create(c => c.IsFullStoBearishCross(14, 3, 3))
        .Or(c => c.IsMacdBearishCross(12, 24, 9))
        .Or(c => c.IsSmaBearishCross(10, 30));

    // Create portfolio instance by using PortfolioBuilder
    var runner = new Builder()
        .Add(fb, 10)
        .Add(aapl, 30)
        .Buy(buyRule)
        .Sell(sellRule)
        .BuyWithAllAvailableCash()
        .FlatExchangeFeeRate(0.001m)
        .Premium(1)
        .Build();
    
    // Start backtesting with the portfolio
    var result = await runner.RunAsync(10000);

    // Get backtest result for the portfolio
    Console.WriteLine(string.Format("Transaction count: {0:#.##}, P/L ratio: {1:0.##}%, Principal: {2:#}, Total: {3:#}",
        resultresult.Transactions.Count(),
        result.CorrectedProfitLoss * 100,
        result.Principal,
        result.CorrectedBalance));
[Back to content](#Content)

<a name="ImplementYourOwnPattern"></a>
### Implement your own pattern through Extension
    // Implement your pattern by creating a static class for extending IndexedCandle class
    public static class IndexedCandleExtension
    {
        public static bool IsEma10Rising(this IndexedCandle ic)
            => ic.Get<ExponentialMovingAverage>(10).Diff(ic.Index).Tick.IsPositive();

        public static bool IsEma10Dropping(this IndexedCandle ic)
            => ic.Get<ExponentialMovingAverage>(10).Diff(ic.Index).Tick.IsNegative();

        public static bool IsEma10BullishCrossEma30(this IndexedCandle ic)
            => ic.Get<ExponentialMovingAverageOscillator>(10, 30).ComputeNeighbour(ic.Index).IsTrue(
                (prev, current, next) => prev.Tick.IsNegative() && current.Tick.IsPositive());

        public static bool IsEma10BearishCrossEma30(this IndexedCandle ic)
            => ic.Get<ExponentialMovingAverageOscillator>(10, 30).ComputeNeighbour(ic.Index).IsTrue(
                (prev, current, next) => prev.Tick.IsPositive() && current.Tick.IsNegative());
    }

    // Use case
    var buyRule = Rule.Create(c => c.IsEma10BullishCrossEma30()).And(c => c.IsEma10Rising());
    var sellRule = Rule.Create(c => c.IsEma10BearishCrossEma30()).Or(c => c.IsEma10Dropping());
    var runner = new Builder().Add(candles, 10).Buy(buyRule).Sell(sellRule).Build();
    var result = await runner.RunAsync(10000, 1);
[Back to content](#Content)

<a name="RegisterRuleForGlobalUse"></a>
### Register rule for global use
    // To use your rule in global, you may register it by using RuleRegistry.Register method
    RuleRegistry.Register("IsBelowSmaX", (ic, p) => ic.Get<SimpleMovingAverage>(p[0])[ic.Index].Tick.IsTrue(t => t > ic.Close));

    // Or, using plain text
    RuleRegistry.Register("IsBelowSma30", "ic.Get<SimpleMovingAverage>(30)[ic.Index].Tick.IsTrue(t => t > ic.Close)");

    // Call it using GetRule method from AnalyzeContext
    using (var ctx = new AnalyzeContext(candles))
    {
        var ruleX = ctx.GetRule("IsBelowSmaX", 30); // Substitute parameter to the rule
        var rule30 = ctx.GetRule("IsBelowSma30");

        var ruleXByRuleCreateEval = Rule.Create(ic => ic.Eval("IsBelowSmaX", 30));  // Create rule with indexedCandle Eval

        var isAboveSma30Candles = new SimpleRuleExecutor(ctx, ruleX).Execute();
    }

<a name="ImplementYourOwnImporter"></a>
### Implement your own importer
    // You can also implement your own importer by implementing the IImporter interface
    public class MyImporter : IImporter
    {
        public async Task<IReadOnlyList<IOhlcv>> ImportAsync(string symbol, DateTime? startTime = null, DateTime? endTime = null, PeriodOption period = PeriodOption.Daily, CancellationToken token = default(CancellationToken))
        {
            // Your implementation to return a list of candles
        }
    }
    
    // Use case
    var importer = new MyImporter();
    var candles = await importer.ImportAsync("FB");
[Back to content](#Content)

<a name="ImplementYourOwnIndicatorSimpleType"></a>
### Implement your own indicator - Simple Type
    // You can also implement your own indicator by extending the AnalyzableBase<TInput, TOutput> class
    public class MyIndicator : AnalyzableBase<IOhlcv, AnalyzableTick<decimal?>>
    {
        // Backing indicator for the indicator
        SimpleMovingAverageByTuple _sma;

        public MyIndicator(IEnumerable<IOhlcv> inputs, int periodCount) : base(inputs)
        {
            // Initialize reference indicators, or other required stuff here
            _sma = new SimpleMovingAverageByTuple(inputs.Select(i => i.Close).ToList(), periodCount);
        }

        // Implement the compute algorithm, uses mappedInputs, index & backing indicators for computation here
        protected override AnalyzableTick<decimal?> ComputeByIndexImpl(IReadOnlyList<IOhlcv> mappedInputs, int index)
        {
			return new AnalyzableTick<decimal?>(mappedInputs[index].DateTime, _sma[index]);
		}
    }

    // Use case
    var results = new MyIndicator(candles, 30).Compute();
    foreach (var r in results)
    {
        Console.WriteLine($"{r.DateTime}, {r.Tick.Value}");
    }
[Back to content](#Content)

<a name="ImplementYourOwnIndicatorCummulativeType"></a>
### Implement your own indicator - Cummulative Type
    // You can implement your own indicator by extending the CummulativeAnalyzableBase<TInput, TOutput> class
    public class MyCumulativeIndicator : CumulativeAnalyzableBase<IOhlcv, AnalyzableTick<decimal?>>
    {
        // Backing indicator for the indicator
        readonly SimpleMovingAverageByTuple _sma;

        public MyCumulativeIndicator(IEnumerable<IOhlcv> inputs, int periodCount) : base(inputs)
        {
            // Initialize reference indicators, or other required stuff here
            _sma = new SimpleMovingAverageByTuple(inputs.Select(i => i.Close).ToList(), periodCount);
        }

        // Implement the compute algorithm for index > InitialValueIndex, uses mappedInputs, index, prev analyzable tick & backing indicators for computation here
        protected override AnalyzableTick<decimal?> ComputeCumulativeValue(IReadOnlyList<IOhlcv> mappedInputs, int index, AnalyzableTick<decimal?> prevOutputToMap)
        {
            return new AnalyzableTick<decimal?>(mappedInputs[index].DateTime, _sma[index] + prevOutputToMap.Tick);
        }

        // Same for the above but for index = InitialValueIndex
        protected override AnalyzableTick<decimal?> ComputeInitialValue(IReadOnlyList<IOhlcv> mappedInputs, int index)
        {
            return new AnalyzableTick<decimal?>(mappedInputs[index].DateTime, _sma[index]);
        }

        // You can also override the InitialValueIndex property & ComputeNullValue method if needed
    }

    // Use case
    var results = new MyCumulativeIndicator(candles).Compute();
    foreach (var r in results)
    {
        Console.WriteLine($"{r.DateTime}, {r.Tick.Value}");
    }   
[Back to content](#Content)

<a name="ImplementYourOwnIndicatorMovingAverageType"></a>
### Implement your own indicator - Moving-Average Type
    // You can make use of the GenericMovingAverage class to get rid of implementing MA-related indicator on your own
     public class MyGmaIndicator : AnalyzableBase<IOhlcv, AnalyzableTick<decimal?>>
    {
        GenericMovingAverage _gma;

        public MyGmaIndicator(IEnumerable<IOhlcv> inputs, int periodCount) : base(inputs)
        {
            // parameters: initialValueIndex, initialValueFunction, indexValueFunction, smoothingFactorFunction
			_gma = new GenericMovingAverage(
				i => inputs.Select(ip => ip.Close).ElementAt(i),
				2.0m / (periodCount + 1),
				inputs.Count());
        }

        protected override AnalyzableTick<decimal?> ComputeByIndexImpl(IReadOnlyList<IOhlcv> mappedInputs, int index)
        {
            return new AnalyzableTick<decimal?>(mappedInputs[index].DateTime, _gma[index]);
        }
    }
[Back to content](#Content)

## Backlog
* (???) Dynamically create indicator & rule patterns from text, allows internal call to dynamic generated stuffs
* () Complete other indicators (e.g. Keltner Channels, MA Envelopes, etc.)
* () Complete candlestick patterns
* (-) Add more rule patterns
* () Data-feeding: Add broker adaptor for real-time trade (e.g. interactive broker, etc.)
* () Graphing: Generate stock chart by indicator values
* () Pipelining: Provide simple programming interface for pipelining the real-time process, i.e. Data-feeding -> decision making by auto rule evaluation -> call to broker for trade -> auto adjust trading strategy based on prev decisions made -> (loop)
* () REPL for dynamic indicator creation, rule creation, strategy making, backtesting, etc.
    * State saver & loader
* MORE, MORE AND MORE!!!!

## Powered by
* [CsvHelper](https://github.com/JoshClose/CsvHelper) ([@JoshClose](https://github.com/JoshClose)) : Great library for reading/ writing CSV file
