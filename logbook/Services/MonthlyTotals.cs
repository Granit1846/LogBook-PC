using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace logbook.Services;

/// <summary>Month identity is independent of the UI language.</summary>
public static class MonthlyTotals
{
    private static readonly string[] Ru={"январь","февраль","март","апрель","май","июнь","июль","август","сентябрь","октябрь","ноябрь","декабрь"};
    private static readonly string[] En={"January","February","March","April","May","June","July","August","September","October","November","December"};
    public static bool TryMonth(string label,out DateTime month)
    {
        month=default;
        var match=Regex.Match(label ?? "",@"^(?:Итого\s+за|Total\s+for)\s+(\S+)\s+(\d{4})$",RegexOptions.IgnoreCase);
        if(!match.Success)return false;
        var name=match.Groups[1].Value;
        var index=Array.FindIndex(Ru,x=>string.Equals(x,name,StringComparison.OrdinalIgnoreCase));
        if(index<0)index=Array.FindIndex(En,x=>string.Equals(x,name,StringComparison.OrdinalIgnoreCase));
        // Recover labels saved before localization resources were available.
        if(index<0 && name.StartsWith("Month_",StringComparison.OrdinalIgnoreCase))
            index=Array.FindIndex(En,x=>string.Equals("Month_"+x,name,StringComparison.OrdinalIgnoreCase));
        if(index<0 || !int.TryParse(match.Groups[2].Value,out var year) || year<1901 || year>2999)return false;
        month=new DateTime(year,index+1,1);return true;
    }
    public static string Label(DateTime month,string language="ru")=>language=="en"
        ? $"Total for {En[month.Month-1]} {month.Year}" : $"Итого за {Ru[month.Month-1]} {month.Year}";
    public static bool TryDate(string value,out DateTime date)=>DateTime.TryParseExact(value,
        new[]{"dd.MM.yy","dd.MM.yyyy","yyyy-MM-dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out date);

    public static void Normalize<T>(IList<T> rows,Func<T,string> date,Action<T,string> setDate,
        Func<T,bool> total,Action<T,bool> setTotal,string language)
    {
        var unique=new HashSet<DateTime>();
        var ordered=new List<(T Row,DateTime Key,int Kind,int Index)>();
        for(int i=0;i<rows.Count;i++) {
            var row=rows[i];
            if(TryMonth(date(row),out var month)) {
                setTotal(row,true);
                if(!unique.Add(month))continue;
                setDate(row,Label(month,language));
                ordered.Add((row,month.AddMonths(1).AddDays(-1),1,i));
            } else if(TryDate(date(row),out var day)) {
                // Some old total rows used the month's last date instead of a label.
                if(total(row)) {
                    month=new DateTime(day.Year,day.Month,1);
                    if(!unique.Add(month))continue;
                    setDate(row,Label(month,language));
                    ordered.Add((row,month.AddMonths(1).AddDays(-1),1,i));
                } else ordered.Add((row,day,0,i));
            } else ordered.Add((row,DateTime.MaxValue,2,i)); // Keep unknown/manual rows.
        }
        var result=ordered.OrderBy(x=>x.Key).ThenBy(x=>x.Kind).ThenBy(x=>x.Index).Select(x=>x.Row).ToList();
        if(rows.SequenceEqual(result))return;
        rows.Clear();foreach(var row in result)rows.Add(row);
    }
}
