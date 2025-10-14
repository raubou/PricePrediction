using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
//    [
//  {
//    "session": "NORMAL",
//    "duration": "DAY",
//    "orderType": "MARKET",
//    "cancelTime": "2025-04-25T18:51:16.527Z",
//    "complexOrderStrategyType": "NONE",
//    "quantity": 0,
//    "filledQuantity": 0,
//    "remainingQuantity": 0,
//    "requestedDestination": "INET",
//    "destinationLinkName": "string",
//    "releaseTime": "2025-04-25T18:51:16.527Z",
//    "stopPrice": 0,
//    "stopPriceLinkBasis": "MANUAL",
//    "stopPriceLinkType": "VALUE",
//    "stopPriceOffset": 0,
//    "stopType": "STANDARD",
//    "priceLinkBasis": "MANUAL",
//    "priceLinkType": "VALUE",
//    "price": 0,
//    "taxLotMethod": "FIFO",
//    "orderLegCollection": [
//      {
//        "orderLegType": "EQUITY",
//        "legId": 0,
//        "instrument": {
//          "cusip": "string",
//          "symbol": "string",
//          "description": "string",
//          "instrumentId": 0,
//          "netChange": 0,
//          "type": "SWEEP_VEHICLE"
//        },
//        "instruction": "BUY",
//        "positionEffect": "OPENING",
//        "quantity": 0,
//        "quantityType": "ALL_SHARES",
//        "divCapGains": "REINVEST",
//        "toSymbol": "string"
//      }
//    ],
//    "activationPrice": 0,
//    "specialInstruction": "ALL_OR_NONE",
//    "orderStrategyType": "SINGLE",
//    "orderId": 0,
//    "cancelable": false,
//    "editable": false,
//    "status": "AWAITING_PARENT_ORDER",
//    "enteredTime": "2025-04-25T18:51:16.527Z",
//    "closeTime": "2025-04-25T18:51:16.527Z",
//    "tag": "string",
//    "accountNumber": 0,
//    "orderActivityCollection": [
//      {
//        "activityType": "EXECUTION",
//        "executionType": "FILL",
//        "quantity": 0,
//        "orderRemainingQuantity": 0,
//        "executionLegs": [
//          {
//            "legId": 0,
//            "price": 0,
//            "quantity": 0,
//            "mismarkedQuantity": 0,
//            "instrumentId": 0,
//            "time": "2025-04-25T18:51:16.527Z"
//          }
//        ]
//      }
//    ],
//    "replacingOrderCollection": [
//      "string"
//    ],
//    "childOrderStrategies": [
//      "string"
//    ],
//    "statusDescription": "string"
//  }
//]
    public sealed class Orders 
    {
        public string session { get; set; }
        public string duration { get; set; }
        public string orderType { get; set; }
        public string complexOrderStrategyType { get; set; }
        public float quantity { get; set; }
        public float filledQuantity { get; set; }
        public float remainingQuantity { get; set; }
        public string requestedDestination { get; set; }
        public string destinationLinkName { get; set; }
        public float price { get; set; }
        public List<OrderLegCollection> orderLegCollection { get; set; }
        public string orderStrategyType { get; set; }
        public long orderId { get; set; }
        public bool cancelable { get; set; }
        public bool editable { get; set; }
        public string status { get; set; }
        public string enteredTime { get; set; }
        public string closeTime { get; set; }
        public string cancelTime { get; set; }
        public string releaseTime { get; set; }
        public string tag { get; set; }
        public int accountId { get; set; }
        public float activationPrice { get; set; }
        public float stopPrice { get; set; }
        public List<OrderActivityCollection> orderActivityCollection { get; set; }
    }

}
