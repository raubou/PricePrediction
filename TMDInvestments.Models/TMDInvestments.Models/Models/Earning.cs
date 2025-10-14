using System;
using System.ComponentModel.DataAnnotations;

public sealed class Earning
{
	[Key]
    public long? Id { get; set; }
    public decimal? epsActual { get; set; }
    public decimal? epsEstimate { get; set; }
    public string hour { get; set; }
    public int? quarter { get; set; }
    public decimal? revenueActual { get; set; }
    public decimal? revenueEstimate { get; set; }
    public string symbol { get; set; }
    public string Name { get; set; }
    public int? year { get; set; }
    public DateTime? date { get; set; }
    public DateTime? LastEarningDate { get; set; }
    public DateTime? DateCreated { get; set; }
}