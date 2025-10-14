var objNews = new Object();

(function (objNews) {
    "use strict";
    //alert(window.location.protocol)
    //alert(window.location.host)
    //alert(window.location.port)
    let baseUrl = window.location.protocol + '//' + window.location.host + '/';
    let baseImageURL = window.location.protocol + '//' + window.location.host + '/';
    //let baseUrl = 'https://tradergo.azurewebsites.net/';
    //let baseUrl = 'https://tradergo.com/';
    //let baseImageURL = 'https://www.tradergo.com/';    
    //alert(baseUrl)
    //let adsURL = 'https://www.bartergora.com/';
    let resetD;
    let minutes = 30;
    let rssModel = new Object();
    //let rssModelAds = new Object();
    let currentD;
    let ip = "";
    let latitude = 0;
    let longitude = 0;
    let browser;
    let info = {};
    let rnd;
    let banners;
    let banner;
    //let currentDateTime;
    let storedDate;
    //let diff;
    let category = 'finance';
    let start = 0;
    let length = 100;
    //let globalBanners = [];
    $(document).ready(function () {
        //currentDateTime = new Date();
        let loc = window.location.toString(); 
        //alert(loc.indexOf("Article"))
        if (loc.indexOf("Article") <= -1)
        objNews.init();
        //alert(window.location.pathname);
    });

    objNews.init = function () {
        //test remove cache
       window.localStorage.clear();
            objNews.GetByCategory();
        }

    objNews.LoadContext = function (banner) {
        //alert("load context")
        if (banner.rssModel.items.length > 0) {
            banner.rssModel.items = objNews.Shuffle(banner.rssModel.items);
            if (window.location.href.toLowerCase().indexOf("travel") < 0)
                objAds.SetLineAd(1, banner.rssModel);
        }
         storedDate = new Date();
        localStorage.setItem("storedDate", storedDate.toString());
        //alert(localStorage.getItem("storedDate"));
    };
        
    objNews.Shuffle = function (items) {
        let n = items.length;
        let itemsNew = new Object();
        while (n > 1) {
            n--;
            let k = Math.floor(Math.random() * n);
            itemsNew = items[k];
            items[k] = items[n];
            items[n] = itemsNew;
            //alert(JSON.stringify(itemsNew));
        }
        return items
    }

    objNews.SaveImpressionsAndClicks = function (title, advertisor, link) {
        var data = {
            Platform: info.TimeZone + ' ' + info.System + ' ' + info.Browser + ' ' + info.Language + ' ' + info.Latitude + ' ' + info.Longitude,
            Title: title,
            Advertisor: advertisor,
            Link: link,
            Impressions: 0,
            Clicks: 1
        };

        $.ajax({
            method: "POST",
            dataType: "json",
            contentType: "application/json",
            url: baseUrl + "../api/TraderGoAPI/SaveImpressionsAndClicks",
            data: JSON.stringify(data),
            error: function (xhr, ajaxOptions, thrownError) {
                //alert(xhr.status);
                //alert(thrownError);
            }
        })
    };

    objNews.SendMail = function (data) {
        $.ajax({
            method: "POST",
            dataType: "json",
            contentType: "application/json",
            url: baseUrl + "../api/TraderGoAPI/SendMail",
            data: JSON.stringify(data),
            error: function (xhr, ajaxOptions, thrownError) {
                //alert(xhr.status);
                //alert(thrownError);
            }
        })
    };


    objNews.FetchCategory = function () {
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
                "Content-Type": "application/json",
                //"Authorization": "Bearer " + polygonToken
            },
            //redirect: "follow", // manual, *follow, error
            //referrerPolicy: "no-referrer", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
            //body: JSON.stringify(data), // body data type must match "Content-Type" header
        }
        //alert(baseUrl + "api/TraderGoAPI/GetByCategory/" + category + "/" + start + "/" + length)
        //fetch(baseUrl + "api/TraderGoAPI/GetByCategory/" + category + "/" + start + "/" + length, options)
        fetch(baseUrl + "api/TraderGoAPI/GetNews", options)
            .then(response => {
                return response.text();
            }).then(data => {
                //alert(JSON.stringify(data))
                rssModel = JSON.parse(data);
                //objNews.CreateCardContent(rssModel, function (show) {
                //    objNews.ShowHideOverLay(show);
                //});
                //rssModel.items = objNews.Shuffle(rssModel.items);
                objNews.CreateCardContent(rssModel);
                //alert(JSON.stringify(rssModel))
                localStorage.setItem(category, JSON.stringify(rssModel))
                localStorage.setItem("resetD", new Date(currentD.getTime() + minutes * 60000))
            })//.catch(function (error) {
            //    console.log("error : " + error);
            //});
    }

    objNews.GetByCategory = function () {//alert("getcategory")
        try {
            //alert(callBack)
            currentD = new Date();
            if (localStorage.getItem("resetD") == null || localStorage.getItem(category) == null) {
                //alert("Fetching New")
                objNews.FetchCategory();
            }
            else {
                resetD = new Date(localStorage.getItem("resetD"));
                //alert(resetD.getTime() + ' ' + currentD.getTime());
                if (currentD.getTime() > resetD.getTime()) {
                    //alert('refreshing new')
                    objNews.FetchCategory();
                }
                else {
                    //alert("Retrieving cache " + category)
                    rssModel = JSON.parse(localStorage.getItem(category));
                    //alert(rssModel)
                    //objNews.CreateCardContent(rssModel, function (show) {
                    //    objNews.ShowHideOverLay(show);
                    //});
                    objNews.CreateCardContent(rssModel);
                    //alert("Retrieved cache " + rssModel.items[0].title)
                }
            }
        } catch (e) {
            objNews.ShowHideOverLay(false);
            console.log(e);
        }
    }

    //objNews.CreateCardContent = function (rssModel, callBack) {
    objNews.CreateCardContent = function (rssModel) {
        //callBack(true);
        let content = "";
        //let length = rssModel.items.length;
        //let index = 0;
        //alert(length);
        //let cardRow = $("<div class='row row-cols-1 row-cols-md-2 g-4'></div>");
        let cardRow1 = $("<div class='row row-cols-1 row-cols-sm-2 row-cols-md-4'></div>");
        //let cardRow2 = $("<div class='row row-cols-1 row-cols-sm-2 row-cols-md-4'></div>");
        $("#containBody").empty();
        //$("#containBody1").empty();
        //$("#containBody").html("<br>");
        //for (var item in rssModel.items) {
        for (var item in rssModel) {
            //let rawContent = "";
            //let preByte = (item.content != '') ? rssModel.items[item].content : (rssModel.items[item].source != '') ? rssModel.items[item].source : rssModel.items[item].title;
            //let utf8Encode = new TextEncoder();
            //let bytes = utf8Encode.encode(preByte)
            //for (var b in bytes) {
            //    rawContent += b + ',';
            //}
            //rawContent = (rawContent.Length > 0) ? rawContent.substr(0, (rawContent.Length - 1)) : rawContent;

            ////if (rssModel.items[item].content != '') {
            //content = rssModel.items[item].content;
            //}
            //else {
            //    content += rssModel.items[item].source;
            //}
            let cardCol = $("<div class='col'></div>");
            let card = $("<div class='cardNews'></div>");
            let divImg = $("<div class='card-div-img'></div>");
            //let divSocial = $("<div class='card-divSocial'></div>")
            //let divFacebook = $("<a class='fa fa-facebook'></a>").attr("href", "javascript:SendFeed('" + rssModel.items[item].link + "')")
            //let divTwitter = $("<a class='fa fa-twitter'></a>").attr("target", "_blank").attr("href", "https://twitter.com/intent/tweet?text=" + rssModel.items[item].title + "&url=" + rssModel.items[item].link);
            //let divMail = $("<a class='fa fa-envelope'></a>").attr("href", "mailto:?subject=rssModel.items[item].title&body=rssModel.items[item].link");
            let Img = $("<img class='card-img-top'>");
            let cardBody = $("<div class='card-body'></div>");
            let h5 = $("<h5 class='card-title'></h5>");
            let p = $("<p class='card-text'></p>");
            
            Img.attr("src", rssModel[item].image)
            //let imageURL = (rssModel.items[item].title.length > 15) ? rssModel.items[item].title.substr(0, 15).replaceAll("https://", "").replaceAll("http://", "").replaceAll("&", "").replaceAll("?", "").replaceAll("=", "").replaceAll("/", "").replaceAll(",", "").replaceAll(".", "").replaceAll(":", "").replaceAll("'", "").replaceAll("`", "").replaceAll(" ", "").replaceAll("'", "").replaceAll("\"", "") + ".png" : rssModel.items[item].title.replaceAll("https://", "").replaceAll("http://", "").replaceAll("&", "").replaceAll("?", "").replaceAll("=", "").replaceAll("/", "").replaceAll(",", "").replaceAll(".", "").replaceAll(":", "").replaceAll("'", "").replaceAll("`", "").replaceAll(" ", "").replaceAll("'", "").replaceAll("\"", "") + ".png"
            //Img.attr("src", baseImageURL + "images/" + imageURL)
                .attr("title", rssModel[item].headline)
                .attr("alt", rssModel[item].headline)
                //.attr("onclick", "javascript:objNews.SaveImpressionsAndClicks('" + rssModel.items[item].title.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item]._source.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item].link + "');window.open('../../Article/" + rssModel.items[item].newsId + "/" + rssModel.items[item].title.replaceAll(" ","-") + "');");
                //.attr("onclick", "javascript:objNews.SaveImpressionsAndClicks('" + rssModel[item].headLine.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel[item].source.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel[item].url + "');window.open('" + rssModel[item].url + "');");
                .attr("onclick", "window.open('" + rssModel[item].url + "');");
            divImg.append(Img);
            //divSocial.append(divFacebook);
            //divSocial.append(divTwitter);
            //divSocial.append(divMail);
            //divImg.append(divSocial);
            h5.html(rssModel[item].headline)
                //.attr("onclick", "javascript:objNews.SaveImpressionsAndClicks('" + rssModel.items[item].title.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item]._source.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item].link + "');window.open('../../Article/" + rssModel.items[item].newsId + "/" + rssModel.items[item].title.replaceAll(" ", "-") + "');");
                //.attr("onclick", "javascript:objNews.SaveImpressionsAndClicks('" + rssModel.items[item].headLine.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item].source.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item].url + "');window.open('" + rssModel.items[item].url + "');");
                .attr("onclick", "window.open('" + rssModel[item].url + "');");
            //p.html(content);
            p.append(rssModel[item].summary);
            //cardBody.empty();
            cardBody.append(h5);
            cardBody.append(p);
            card.empty();
            //card.attr("onclick", "javascript:objNews.SaveImpressionsAndClicks('" + rssModel.items[item].title.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item]._source.replace(/'/g, "").replace(/,/g, "") + "','" + rssModel.items[item].link + "');window.open('" + rssModel.items[item].link + "');")
            card.append(divImg);
            card.append(cardBody);
            //cardCol.empty();
            cardCol.append(card);
            cardRow1.append(cardCol);             
        }
        
        $("#newsContent").append(cardRow1);
        objNews.ShowHideOverLay(false);
    }

    objNews.ShowHideOverLay = function (show) {
        //alert(show);
        setTimeout(function () {
            if (show)
                $('.overlay').show();
            else
                $('.overlay').hide();
        }, 100);
    }

})(objNews);