using System;
using System.Collections.Generic;
using System.Text;

namespace OKExSDK.Models.Spot
{
    public class SpotOrder
    {
        /// <summary>
        /// 由您设置的订单ID来识别您的订单
        /// </summary>
        public string client_oid { get; set; }
        /// <summary>
        /// limit，market(默认是limit)
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// buy or sell
        /// </summary>
        public string side { get; set; }
        /// <summary>
        /// 币对名称
        /// </summary>
        public string instrument_id { get; set; }
        /// <summary>
        /// 下单类型(当前为币币交易，请求值为1)
        /// </summary>
        public int margin_trading { get; set; } = 1;
        /// <summary>
        /// 买入或卖出的数量
        /// </summary>
        public string size { get; set; }
    }
}
