//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace CoreLibrary.DB
{
    using System;
    using System.Collections.Generic;
    
    public partial class order
    {
        public string orderid { get; set; }
        public Nullable<decimal> price { get; set; }
        public Nullable<decimal> amount { get; set; }
        public Nullable<decimal> createdate { get; set; }
        public string status { get; set; }
        public string symbol { get; set; }
        public string side { get; set; }
        public Nullable<decimal> fees { get; set; }
        public string type { get; set; }
        public string platform { get; set; }
        public string date { get; set; }
    }
}
