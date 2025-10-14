using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TMDInvestment.Models
{
    public sealed class AccountInfo
    {
		//[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		//public int Id { get; set; }
		[Required]
		public int UserId { get; set; }
		[DisplayName("Use Day Trade")]
		[Required(ErrorMessage = "{0} Required")]
		public bool UseDayTrade { get; set; }
		[DisplayName("Use Swing Trade")]
		[Required(ErrorMessage = "{0} Required")]
		public bool UseSwingTrade { get; set; }
		[DisplayName("History Day Range")]
		[Required(ErrorMessage = "{0} Required")]
		public int? HistoryDayRange { get; set; }
		[DisplayName("History Interval Range")]
		[Required(ErrorMessage = "{0} Required")]
		public int? HistoryIntervalRange { get; set; }
		[DisplayName("Percent Balance Use")]
		[Required(ErrorMessage = "{0} Required")]
		public double? PercentBalanceUse { get; set; }
		[DisplayName("Price Range Max")]
		[Required(ErrorMessage = "{0} Required")]
		public double? PriceRangeMax { get; set; }
		[DisplayName("Price Range Min")]
		[Required(ErrorMessage = "{0} Required")]
		public double? PriceRangeMin { get; set; }
		[DisplayName("Percent Range High")]
		[Required(ErrorMessage = "{0} Required")]
		public double? PercentRangeHigh { get; set; }
		[DisplayName("Percent Range Low")]
		[Required(ErrorMessage = "{0} Required")]
		public double? PercentRangeLow { get; set; }
		[DisplayName("Squeeze Range High")]
		[Required(ErrorMessage = "{0} Required")]
		public double? SqueezeRangeHigh { get; set; }
		[DisplayName("Squeeze Range Low")]
		[Required(ErrorMessage = "{0} Required")]
		public double? SqueezeRangeLow { get; set; }
		[DisplayName("Volume Range High")]
		[Required(ErrorMessage = "{0} Required")]
		public long? VolumeRangeHigh { get; set; }
		[DisplayName("Volume Range Low")]
		[Required(ErrorMessage = "{0} Required")]
		public long? VolumeRangeLow { get; set; }
		[DisplayName("Max Hold time for a Stock")]
		[Required(ErrorMessage = "{0} Required")]
		public int? MaxHoldforStock { get; set; }
		[Required(ErrorMessage = "{0} Required")]
		public int? MaxRoundTradesPerDay { get; set; }
		//[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		//public DateTime DateCreated { get; set; }
		//public DateTime DateUpdated { get; set; }
	}
}
