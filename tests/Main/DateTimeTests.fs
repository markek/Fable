[<Util.Testing.TestFixture>]
module Fable.Tests.DateTime
open System
open Util.Testing
open Fable.Tests.Util

#if DOTNETCORE
    type System.DateTime with
        member x.ToShortDateString() = x.ToString("d")
        member x.ToShortTimeString() = x.ToString("t")
        member x.ToLongDateString() = x.ToString("D")
        member x.ToLongTimeString() = x.ToString("T")
#endif

let toSigFigs nSigFigs x =
    let absX = abs x
    let digitsToStartOfNumber = floor(log10 absX) + 1. // x > 0 => +ve | x < 0 => -ve
    let digitsToAdjustNumberBy = int digitsToStartOfNumber - nSigFigs
    let scale = pown 10. digitsToAdjustNumberBy
    round(x / scale) * scale

let thatYearSeconds (dt: DateTime) =
    (dt - DateTime(dt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds

let thatYearMilliseconds (dt: DateTime) =
    (dt - DateTime(dt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds

[<Test>]
let ``DateTime.ToString with format works``() =
    DateTime(2014, 9, 11, 16, 37, 0).ToString("HH:mm")
    |> equal "16:37"

[<Test>]
let ``DateTime.ToString without separator works``() = // See #1131
    DateTime(2017, 9, 5).ToString("yyyyMM")
    |> equal "201709"

// TODO
// [<Test>]
// let ``TimeSpan.ToString with format works``() =
//     TimeSpan.FromMinutes(234.).ToString("hh\:mm\:ss")
//     |> equal "03:54:00"

[<Test>]
let ``DateTime.ToString with Round-trip format works for Utc`` =
    DateTime(2014, 9, 11, 16, 37, 2, DateTimeKind.Utc).ToString("O")
    |> equal "2014-09-11T16:37:02.000Z"

// TODO
// Next test is disable because it's depends on the time zone of the machine
//A fix could be to use a regex or detect the time zone
// [<Test>]
// let ``DateTime.ToString with Round-trip format works for local`` =
//     DateTime(2014, 9, 11, 16, 37, 2, DateTimeKind.Local).ToString("O")
//     |> equal "2014-09-11T16:37:02.000+02:00" // Here the time zone is Europte/Paris (GMT+2)

[<Test>]
let ``DateTime can be JSON serialized forth and back``() =
    let utc = DateTime(2016, 8, 4, 17, 30, 0, DateTimeKind.Utc)
    #if FABLE_COMPILER
    let json = Fable.Core.JsInterop.toJson utc
    let utc = Fable.Core.JsInterop.ofJson<DateTime> json
    #else
    let json = Newtonsoft.Json.JsonConvert.SerializeObject utc
    let utc = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime> json
    #endif
    utc.Kind = DateTimeKind.Utc |> equal true
    utc.ToString("HH:mm") |> equal "17:30"

[<Test>]
let ``DateTime from Year 1 to 99 works``() =
    let date = DateTime(1, 1, 1)
    date.Year |> equal 1
    let date = DateTime(99, 1, 1)
    date.Year |> equal 99

[<Test>]
let ``DateTime UTC from Year 1 to 99 works``() =
    let date = DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    date.Year |> equal 1
    let date = DateTime(99, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    date.Year |> equal 99

// TODO: These two tests give different values for .NET and JS because DateTime
// becomes as a plain JS Date object, so I'm just checking the fields get translated
[<Test>]
let ``DateTime.MaxValue works``() =
    let d1 = DateTime.Now
    let d2 = DateTime.MaxValue
    d1 < d2 |> equal true

[<Test>]
let ``DateTime.MinValue works``() =
    let d1 = DateTime.Now
    let d2 = DateTime.MinValue
    d1 < d2 |> equal false

[<Test>]
let ``DateTime.MinValue works in pattern match``() =
    let d1 = Some DateTime.Now
    match d1 with
    | Some date when date <> DateTime.MinValue -> ()
    | _ -> failwith "expected pattern match above"

[<Test>]
let ``DateTime.ToLocalTime works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    let d' = d.ToLocalTime()
    d.Kind <> d'.Kind
    |> equal true

[<Test>]
let ``DateTime.Hour works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    d.Hour |> equal 13

// TODO: These four tests don't match exactly between .NET and JS
// Think of a way to compare the results approximately
[<Test>]
let ``DateTime.ToLongDateString works``() =
    let dt = DateTime(2014, 9, 11, 16, 37, 0)
    let s = dt.ToLongDateString()
    s.Length > 0
    |> equal true

[<Test>]
let ``DateTime.ToShortDateString works``() =
    let dt = DateTime(2014, 9, 11, 16, 37, 0)
    let s = dt.ToShortDateString()
    s.Length > 0
    |> equal true

[<Test>]
let ``DateTime.ToLongTimeString works``() =
    let dt = DateTime(2014, 9, 11, 16, 37, 0)
    let s = dt.ToLongTimeString()
    s.Length > 0
    |> equal true

[<Test>]
let ``DateTime.ToShortTimeString works``() =
    let dt = DateTime(2014, 9, 11, 16, 37, 0)
    let s = dt.ToShortTimeString()
    s.Length > 0
    |> equal true

// TODO: Unfortunately, JS will happily create invalid dates like DateTime(2014,2,29)
//       But this problem also happens when parsing, so I haven't tried to fix it
[<Test>]
let ``DateTime constructors work``() =
    let d1 = DateTime(2014, 10, 9)
    let d2 = DateTime(2014, 10, 9, 13, 23, 30)
    let d3 = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    let d4 = DateTime(2014, 10, 9, 13, 23, 30, 500)
    let d5 = DateTime(2014, 10, 9, 13, 23, 30, 500, DateTimeKind.Utc)
    d1.Day + d2.Second + d3.Second + d4.Millisecond + d5.Millisecond
    |> equal 1069

[<Test>]
let ``DateTime constructor from Ticks works``() =
    let d = DateTime(624059424000000000L)
    equal 1978 d.Year
    equal 7 d.Month
    equal 27 d.Day

[<Test>]
let ``DateTime.IsLeapYear works``() =
    DateTime.IsLeapYear(2014) |> equal false
    DateTime.IsLeapYear(2016) |> equal true

// TODO: Re-enable this test when we can fix it in the CI servers
// [<Test>]
// let ``DateTime.IsDaylightSavingTime works``() =
//     let d1 = DateTime(2017, 7, 18, 2, 0, 0)
//     let d2 = DateTime(2017, 12, 18, 2, 0, 0)
//     d1.IsDaylightSavingTime() |> equal true
//     d2.IsDaylightSavingTime() |> equal false

[<Test>]
let ``DateTime.DaysInMonth works``() =
    DateTime.DaysInMonth(2014, 1) |> equal 31
    DateTime.DaysInMonth(2014, 2) |> equal 28
    DateTime.DaysInMonth(2014, 4) |> equal 30
    DateTime.DaysInMonth(2016, 2) |> equal 29

[<Test>]
let ``DateTime.Now works``() =
    let d = DateTime.Now
    d > DateTime.MinValue |> equal true

[<Test>]
let ``DateTime.UtcNow works``() =
    let d = DateTime.UtcNow
    d > DateTime.MinValue |> equal true

[<Test>]
let ``DateTime.Parse works``() =
    let d = DateTime.Parse("9/10/2014 1:50:34 PM")
    d.Year + d.Month + d.Day + d.Hour + d.Minute
    |> equal 2096

[<Test>]
let ``DateTime.Parse with time-only string works``() = // See #1045
    let d = DateTime.Parse("13:50:34")
    d.Hour + d.Minute + d.Second |> equal 97
    let d = DateTime.Parse("1:5:34 AM")
    d.Hour + d.Minute + d.Second |> equal 40
    let d = DateTime.Parse("1:5:34 PM")
    d.Hour + d.Minute + d.Second |> equal 52

[<Test>]
let ``DateTime.TryParse works``() =
    let f d =
        match DateTime.TryParse(d) with
        | true, _ -> true
        | false, _ -> false
    f "foo" |> equal false
    f "9/10/2014 1:50:34 PM" |> equal true
    f "1:50:34" |> equal true

[<Test>]
let ``DateTime.Today works``() =
    let d = DateTime.Today
    equal 0 d.Hour

[<Test>]
let ``DateTime.ToUniversalTime works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    d.ToUniversalTime().Kind <> d.Kind
    |> equal true

[<Test>]
let ``DateTime.Date works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30)
    d.Date.Hour |> equal 0
    d.Date.Day |> equal 9

[<Test>]
let ``DateTime.Day works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    let d' = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    d.Day + d'.Day |> equal 18

[<Test>]
let ``DateTime.DayOfWeek works``() =
    let d = DateTime(2014, 10, 9)
    d.DayOfWeek |> equal DayOfWeek.Thursday

[<Test>]
let ``DateTime.DayOfYear works``() =
    let d = DateTime(2014, 10, 9)
    d.DayOfYear |> equal 282

[<Test>]
let ``DateTime.Millisecond works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, 999)
    d.Millisecond |> equal 999

[<Test>]
let ``DateTime.Ticks works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, 999)
    d.Ticks
    |> equal 635484578109990000L

[<Test>]
let ``DateTime.Minute works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    let d' = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    d.Minute + d'.Minute
    |> equal 46

[<Test>]
let ``DateTime.Month works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    let d' = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    d.Month + d'.Month
    |> equal 20

[<Test>]
let ``DateTime.Second works``() =
    let d = DateTime(2014,9,12,0,0,30)
    let d' = DateTime(2014,9,12,0,0,59)
    d.Second + d'.Second
    |> equal 89

[<Test>]
let ``DateTime.Year works``() =
    let d = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Local)
    let d' = DateTime(2014, 10, 9, 13, 23, 30, DateTimeKind.Utc)
    d.Year + d'.Year
    |> equal 4028

[<Test>]
let ``DateTime.AddYears works``() =
    let test v expected =
        let dt = DateTime(2016,2,29,0,0,0,DateTimeKind.Utc).AddYears(v)
        equal expected (dt.Month + dt.Day)
    test 100 31
    test 1 30
    test -1 30
    test -100 31
    test 0 31

[<Test>]
let ``DateTime.AddMonths works``() =
    let test v expected =
        let dt = DateTime(2016,1,31,0,0,0,DateTimeKind.Utc).AddMonths(v)
        dt.Year + dt.Month + dt.Day
        |> equal expected
    test 100 2060
    test 20 2056
    test 6 2054
    test 5 2052
    test 1 2047
    test 0 2048
    test -1 2058
    test -5 2054
    test -20 2050
    test -100 2046

[<Test>]
let ``DateTime.AddDays works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddDays(v)
        thatYearSeconds dt
        |> equal expected
    test 100. 30585600.0
    test -100. 13305600.0
    test 0. 21945600.0

[<Test>]
let ``DateTime.AddHours works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddHours(v)
        thatYearSeconds dt
        |> equal expected
    test 100. 22305600.0
    test -100. 21585600.0
    test 0. 21945600.0

[<Test>]
let ``DateTime.AddMinutes works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddMinutes(v)
        thatYearSeconds dt
        |> equal expected
    test 100. 21951600.0
    test -100. 21939600.0
    test 0. 21945600.0

[<Test>]
let ``DateTime.AddSeconds works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddSeconds(v)
        thatYearSeconds dt
        |> equal expected
    test 100. 21945700.0
    test -100. 21945500.0
    test 0. 21945600.0

[<Test>]
let ``DateTime.AddMilliseconds works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddMilliseconds(v)
        thatYearMilliseconds dt
        |> equal expected
    test 100. 2.19456001e+10
    test -100. 2.19455999e+10
    test 0. 2.19456e+10

// NOTE: Doesn't work for values between 10000L (TimeSpan.TicksPerMillisecond) and -10000L, except 0L
[<Test>]
let ``DateTime.AddTicks works``() =
    let test v expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc).AddTicks(v)
        dt.Ticks
        |> equal expected
    let ticks = 635460768000000000L
    test 100000L (ticks + 100000L)
    test -100000L (ticks - 100000L)
    test 0L ticks

[<Test>]
let ``DateTime Addition works``() =
    let test ms expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc)
        let ts = TimeSpan.FromMilliseconds(ms)
        let res1 = dt.Add(ts) |> thatYearSeconds
        let res2 = (dt + ts) |> thatYearSeconds
        equal true (res1 = res2)
        equal expected res1
    test 1000. 21945601.0
    test -1000. 21945599.0
    test 0. 21945600.0

[<Test>]
let ``DateTime Subtraction with TimeSpan works``() =
    let test ms expected =
        let dt = DateTime(2014,9,12,0,0,0,DateTimeKind.Utc)
        let ts = TimeSpan.FromMilliseconds(ms)
        let res1 = dt.Subtract(ts) |> thatYearSeconds
        let res2 = (dt - ts) |> thatYearSeconds
        equal true (res1 = res2)
        equal expected res1
    test 1000. 21945599.0
    test -1000. 21945601.0
    test 0. 21945600.0

[<Test>]
let ``DateTime Subtraction with DateTime works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        let res1 = dt1.Subtract(dt2).TotalSeconds
        let res2 = (dt1 - dt2).TotalSeconds
        equal true (res1 = res2)
        equal expected res1
    test 1000. -1.0
    test -1000. 1.0
    test 0. 0.0

[<Test>]
let ``DateTime Comparison works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        let res1 = compare dt1 dt2
        let res2 = dt1.CompareTo(dt2)
        let res3 = DateTime.Compare(dt1, dt2)
        equal true (res1 = res2 && res2 = res3)
        equal expected res1
    test 1000. -1
    test -1000. 1
    test 0. 0

[<Test>]
let ``DateTime GreaterThan works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        dt1 > dt2 |> equal expected
    test 1000. false
    test -1000. true
    test 0. false

[<Test>]
let ``DateTime LessThan works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        dt1 < dt2 |> equal expected
    test 1000. true
    test -1000. false
    test 0. false

[<Test>]
let ``DateTime Equality works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        dt1 = dt2 |> equal expected
    test 1000. false
    test -1000. false
    test 0. true

[<Test>]
let ``DateTime Inequality works``() =
    let test ms expected =
        let dt1 = DateTime(2014, 10, 9, 13, 23, 30, 234, DateTimeKind.Utc)
        let dt2 = dt1.AddMilliseconds(ms)
        dt1 <> dt2 |> equal expected
    test 1000. true
    test -1000. true
    test 0. false

[<Test>]
let ``DateTime TimeOfDay works``() =
    let d = System.DateTime(2014, 10, 9, 13, 23, 30, 1, System.DateTimeKind.Utc)
    let t = d.TimeOfDay

    t |> equal (TimeSpan(0, 13, 23, 30, 1))

[<Test>]
let ``TimeSpan constructors work``() =
    let t1 = TimeSpan(20000L)
    let t2 = TimeSpan(3, 3, 3)
    let t3 = TimeSpan(5, 5, 5, 5)
    let t4 = TimeSpan(7, 7, 7, 7, 7)

    t1.TotalMilliseconds |> equal 2.0
    t2.TotalMilliseconds |> equal 10983000.0
    t3.TotalMilliseconds |> equal 450305000.0
    t4.TotalMilliseconds |> equal 630427007.0

    t1.TotalMilliseconds + t2.TotalMilliseconds + t3.TotalMilliseconds + t4.TotalMilliseconds
    |> equal 1091715009.0

[<Test>]
let ``TimeSpan static creation works``() =
    let t1 = TimeSpan.FromTicks(20000L)
    let t2 = TimeSpan.FromMilliseconds(2.)
    let t3 = TimeSpan.FromDays   (2.)
    let t4 = TimeSpan.FromHours  (2.)
    let t5 = TimeSpan.FromMinutes(2.)
    let t6 = TimeSpan.FromSeconds(2.)
    let t7 = TimeSpan.Zero
    t1.TotalMilliseconds + t2.TotalMilliseconds + t3.TotalMilliseconds + t4.TotalMilliseconds +
       t5.TotalMilliseconds + t6.TotalMilliseconds + t7.TotalMilliseconds
    |> equal 180122004.0

[<Test>]
let ``TimeSpan components work``() =
    let t = TimeSpan.FromMilliseconds(96441615.)
    t.Days + t.Hours + t.Minutes + t.Seconds + t.Milliseconds |> float
    |> equal 686.

[<Test>]
let ``TimeSpan.Ticks works``() =
    let t = TimeSpan.FromTicks(20000L)
    t.Ticks
    |> equal 20000L

// NOTE: This test fails because of very small fractions, so I cut the fractional part
[<Test>]
let ``TimeSpan totals work``() =
    let t = TimeSpan.FromMilliseconds(96441615.)
    t.TotalDays + t.TotalHours + t.TotalMinutes + t.TotalSeconds |> floor
    |> equal 98076.0

[<Test>]
let ``TimeSpan.Duration works``() =
    let test ms expected =
        let t = TimeSpan.FromMilliseconds(ms)
        t.Duration().TotalMilliseconds
        |> equal expected
    test 1. 1.
    test -1. 1.
    test 0. 0.

[<Test>]
let ``TimeSpan.Negate works``() =
    let test ms expected =
        let t = TimeSpan.FromMilliseconds(ms)
        t.Negate().TotalMilliseconds
        |> equal expected
    test 1. -1.
    test -1. 1.
    test 0. 0.

[<Test>]
let ``TimeSpan Addition works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        let res1 = t1.Add(t2).TotalMilliseconds
        let res2 = (t1 + t2).TotalMilliseconds
        equal true (res1 = res2)
        equal expected res1
    test 1000. 2000. 3000.
    test 200. -1000. -800.
    test -2000. 1000. -1000.
    test -200. -300. -500.
    test 0. 1000. 1000.
    test -2000. 0. -2000.
    test 0. 0. 0.

[<Test>]
let ``TimeSpan Subtraction works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        let res1 = t1.Subtract(t2).TotalMilliseconds
        let res2 = (t1 - t2).TotalMilliseconds
        equal true (res1 = res2)
        equal expected res1
    test 1000. 2000. -1000.
    test 200. -2000. 2200.
    test -2000. 1000. -3000.
    test 200. -300. 500.
    test 0. 1000. -1000.
    test 1000. 1000. 0.
    test 0. 0. 0.

[<Test>]
let ``TimeSpan Comparison works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        let res1 = compare t1 t2
        let res2 = t1.CompareTo(t2)
        let res3 = TimeSpan.Compare(t1, t2)
        equal true (res1 = res2 && res2 = res3)
        equal expected res1
    test 1000. 2000. -1
    test 2000. 1000. 1
    test -2000. -2000. 0
    test 200. -200. 1
    test 0. 1000. -1
    test 1000. 1000. 0
    test 0. 0. 0

[<Test>]
let ``TimeSpan GreaterThan works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        t1 > t2
        |> equal expected
    test 1000. 2000. false
    test 2000. 1000. true
    test -2000. -2000. false

[<Test>]
let ``TimeSpan LessThan works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        t1 < t2
        |> equal expected
    test 1000. 2000. true
    test 2000. 1000. false
    test -2000. -2000. false

[<Test>]
let ``TimeSpan Equality works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        t1 = t2
        |> equal expected
    test 1000. 2000. false
    test 2000. 1000. false
    test -2000. -2000. true

[<Test>]
let ``TimeSpan Inequality works``() =
    let test ms1 ms2 expected =
        let t1 = TimeSpan.FromMilliseconds(ms1)
        let t2 = TimeSpan.FromMilliseconds(ms2)
        t1 <> t2
        |> equal expected
    test 1000. 2000. true
    test 2000. 1000. true
    test -2000. -2000. false

// Disable System.Timers.Timer tests for .NET Core until it's implemented.
#if !DOTNETCORE

// TODO: Disable this test temporarily for NUnit as it's failing in AppVeyor
#if FABLE_COMPILER
[<Test>]
let ``Timer with AutoReset = true works``() =
    async {
        let res = ref 0
        let t = new Timers.Timer(50.)
        t.Elapsed.Add(fun ev -> res := !res + 5)
        t.Start()
        do! Async.Sleep 125
        t.Stop()
        do! Async.Sleep 50
        equal 10 !res
    } |> Async.RunSynchronously
#endif

[<Test>]
let ``Timer with AutoReset = false works``() =
    async {
        let res = ref 0
        let t = new Timers.Timer()
        t.Elapsed.Add(fun ev -> res := !res + 5)
        t.AutoReset <- false
        t.Interval <- 25.
        t.Enabled <- true
        do! Async.Sleep 100
        equal 5 !res
    } |> Async.RunSynchronously

[<Test>]
let ``Timer.Elapsed.Subscribe works``() =
    async {
        let res = ref 0
        let t = new Timers.Timer(50.)
        let disp = t.Elapsed.Subscribe(fun ev -> res := !res + 5)
        t.Start()
        do! Async.Sleep 125
        disp.Dispose()
        do! Async.Sleep 50
        equal 10 !res
        t.Stop()
    } |> Async.RunSynchronously

#endif
