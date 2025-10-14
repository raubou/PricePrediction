let objChart = new Object();
let datasets;
(function () {
    var choiceContainer;
    $(document).ready(function () {
        choiceContainer = $("#choices");
    })
     //var dataSet = [
     //{label: "USA", color: "#005CDE" },
     //{label: "Russia", color: "#005CDE" },
     //{ label: "UK", color: "#00A36A" },
     //{ label: "Germany", color: "#7D0096" },
     //{ label: "Denmark", color: "#992B00" },
     //{ label: "Sweden", color: "#DE000F" },
     //{ label: "Norway", color: "#ED7B00" }
 //];
    // hard-code color indices to prevent them from shifting as
    // countries are turned on/off
    objChart.InitData = function () {
        datasets = {
            //"Low": {
            //    label: "Low",
            //    data: [
            //    ]
            //},
            //"High": {
            //    label: "High",
            //    data: [
            //    ]
            //},
            "Open": {
                label: "History",
                data: [
                ]
            }//,
            //"Close": {
            //    label: "Close",
            //    data: [
            //    ]
            //}
        };
    }
    objChart.SetChart = function (min) {
        //alert(JSON.stringify(datasets))
        var i = 0;
        $.each(datasets, function (key, val) {
            val.color = i;
            ++i;
        });

        // insert checkboxes
        /*        var choiceContainer = $("#choices");*/
        choiceContainer.empty();
        $.each(datasets, function (key, val) {//alert('coco')
            choiceContainer.append('<input type="checkbox" name="' + key +
                '" checked="checked" id="id' + key + '" style="display:none;">' +
                '<label for="id' + key + '" style="display:none;">' +
                val.label + '</label>');
        });
        
        //$('input[name="graphLegion"]').off("click", function () {
        //    plotAccordingToChoices(min)
        //})
        //$('input[name="graphLegion"]').on("click", function () {
        //    plotAccordingToChoices(min)
        //})
        setTimeout(function () {
            //choiceContainer.find("input").click(function () { plotAccordingToChoices(min) });
            plotAccordingToChoices(min);
        }, 1000)
    }

    function plotAccordingToChoices(min) {//alert('pork')
        var data = [];

        choiceContainer.find("input:checked").each(function () {
            var key = $(this).attr("name");
            if (key && datasets[key])
                data.push(datasets[key]);
        });

        if (data.length > 0)
            $.plot($("#placeholder"), data, {
                yaxis: { min: min },
                xaxis: { tickDecimals: 0 }
            });
    }

    
})(objChart)