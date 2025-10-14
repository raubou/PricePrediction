let objSearch = new Object();

(function () {
    "use strict";
    let value;

    $(document).ready(function () {
        $(document).on("keyup", function (e) {            
            let value = $.trim($("#txtSearch").val());
            //alert(value.length)
            if (/*e.key === 'Enter' &&*/ $("#txtSearch").is(":focus") && value.length >= 3) {
                value = encodeURIComponent($.trim($("#txtSearch").val().replace(/'/g, "")));
                //objContext.SearchResults(value);
                objContext.Search(value);
            } else if ($.trim($("#txtSearch").val()) == '') {
                objSearch.ShowHideResults(false);
            }
            objSearch.ScrolltoTop();           
        });
       
        $("#txtSearch").on("keyup keydown keypress input", function () {
            if ($.trim($("#txtSearch").val()) == '') {
                objSearch.ShowHideResults(false);
            }
        });

        $("#btnSearch").on("click", function () {
            if ($.trim($("#txtSearch").val()) != '') {
                value = encodeURIComponent($.trim($("#txtSearch").val().replaceAll(/'/g, "")));
                //objContext.SearchResults(value);
                objContext.Search(value);
            } else if ($.trim($("#txtSearch").val()) == '') {
                objSearch.ShowHideResults(false);
            }
            objSearch.ScrolltoTop();
        });
    });

    objSearch.SearchTrends = async function () {//alert('hello')
        if ($.trim($("#direction").val()) == '') {
            $("#direction").addClass("border-danger")
            $("#alertText").html("<strong>Direction</strong> Is a required field.");
            $("#siteElerts").addClass("show")
            $("#siteElerts").css("display","block")
            return
        } else {
            $("#direction").removeClass("border-danger")
            $("#alertText").html("");
            $("#siteElerts").removeClass("show")
            $("#siteElerts").css("display", "none")
        }
        if ($.trim($("#datepickerFrom").val()) == '') {
            $("#volumeFrom").addClass("border-danger")
            $("#alertText").html("<strong>From Date</strong> Is a required field.");
            $("#siteElerts").addClass("show")
            $("#siteElerts").css("display", "block")
            return
        } else {
            $("#volumeFrom").removeClass("border-danger")
            $("#alertText").html("");
            $("#siteElerts").removeClass("show")
            $("#siteElerts").css("display", "none")
        }
        if ($.trim($("#datepickerTo").val()) == '') {
            $("#volumeFrom").addClass("border-danger")
            $("#alertText").html("<strong>To Date</strong> Is a required field.");
            $("#siteElerts").addClass("show")
            $("#siteElerts").css("display", "block")
            return
        } else {
            $("#volumeFrom").removeClass("border-danger")
            $("#alertText").html("");
            $("#siteElerts").removeClass("show")
            $("#siteElerts").css("display", "none")
        }

        await objContext.SetDates();
        await objContext.GetGroupDaily("../../api/TraderGoAPI/GetGroupDaily/" + toYear + '-' + toMonth + '-' + toDay);
    }

    objSearch.ShowHideResults = function (show) {
        if (show) {
            $("#searchResults").show(500);
        }
        else {
            $("#searchResults").hide(500);
        }
    }

    objSearch.ScrolltoTop = function () {
        $("html, body").animate({ scrollTop: 0 }, "slow");
    }
})(objSearch)