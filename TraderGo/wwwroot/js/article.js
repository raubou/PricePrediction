let objArticle = new Object();
(function (objArticle) {
    let item = new Object();
    $(document).ready(function () {
        objArticle.init();
    });
    objArticle.init = function () {
        if (window.location.pathname.toLowerCase().indexOf('article') > 0)
            objArticle.GetArticle(Id);
    }
    objArticle.GetArticle = function (Id) {
        //alert("fetch from DB")
        fetch("../../api/TraderGoAPI/GetArticleById/" + Id)
            .then(response => {
                return response.text();
            }).then(data => {
                //alert(JSON.stringify(data))
                item = JSON.parse(data);
                $("#published").html(item.pubDate)
                $("#content").html(item.content)
                var img = (item.title.length > 15) ? "//www.tradergo.com/images/" + item.title.substr(0, 15).replaceAll("https://", "").replaceAll("http://", "").replaceAll("&", "").replaceAll("?", "").replaceAll("=", "").replaceAll("/", "").replaceAll(",", "").replaceAll(".", "").replaceAll(":", "").replaceAll("'", "").replaceAll("`", "").replaceAll(" ", "") + ".png" : "//www.wikinewz.net/images/" + item.title.replaceAll("https://", "").replaceAll("http://", "").replaceAll("&", "").replaceAll("?", "").replaceAll("=", "").replaceAll("/", "").replaceAll(",", "").replaceAll(".", "").replaceAll(":", "").replaceAll("'", "").replaceAll("`", "").replaceAll(" ", "") + ".png";
                //alert(img)
                $("#image").prop("src", img)
                //objContext.CreateCardContent(rssModel, function (show) {
                //    objContext.ShowHideOverLay(show);
                //});
                //rssModel.items = objContext.Shuffle(rssModel.items);
                //objContext.CreateCardContent(rssModel);
                //alert(JSON.stringify(rssModel))
                //localStorage.setItem(category, JSON.stringify(rssModel))
                //localStorage.setItem("resetD", new Date(currentD.getTime() + minutes * 60000))
                //objContext.ShowHideOverLay(false);
            }).catch(function (error) {
                console.log("error : " + error);
                //objContext.ShowHideOverLay(false);
            });
    }

})(objArticle);