using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLibrary.DB
{
    public class DbHelper
    {
        private static DbHelper _SingletonSecond = null;

        static DbHelper()
        {

            _SingletonSecond = new DbHelper();
        }

        public static DbHelper CreateInstance()
        {
            return _SingletonSecond;
        }

        public void AddOrder(order order)
        {
            try
            {
                using (var db = new CoinTradeDBEntities())
                {
                    db.order.Add(order);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw (e);

            }
        }
        public void AddUpdateOrder(order order)
        {
            try
            {
                using (var db = new CoinTradeDBEntities())
                {

                    var orderdb = db.order.FirstOrDefault(a => a.orderid == order.orderid);
                    if (orderdb != null)
                    {
                        //orderdb.amount = Math.Round((decimal)(order.amount ?? 0), 4);
                        ////orderdb.createdate = order.createdate;
                        //orderdb.fees = order.fees;
                        ////orderdb.platform = order.platform;
                        //orderdb.price = Math.Round((decimal)(order.price ?? 0), 4);
                        //orderdb.side = order.type;
                        //orderdb.status = order.status;
                        ////orderdb.symbol = order.symbol;
                        ////orderdb.date = DateTime.Now.ToString("yyyyMMddHHmmss");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(order.date))
                        {
                            order.date = DateTime.Now.ToString("yyyyMMddHHmmss");
                        }
                        order.createdate = order.createdate ?? 0;
                        db.order.Add(order);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw (e);

            }
        }
        public List<order> GetDBOrders(decimal createdate = 0)
        {
            List<order> list = new List<order>();
            try
            {
                using (var db = new CoinTradeDBEntities())
                {
                    var orders = db.order.Where(a => a.createdate >= createdate).ToList();
                    if (orders != null && orders.Count() > 0)
                    {
                        list = orders;
                    }
                }
            }
            catch (Exception e)
            {
                throw (e);

            }
            return list;
        }
        public List<order> GetDBOrders(DateTime opendate)
        {
            List<order> list = new List<order>();
            try
            {
                string str = opendate.ToString("yyyyMMddHHmmss");
                using (var db = new CoinTradeDBEntities())
                {
                    var orders = db.order.Where(a => str.CompareTo(a.date) <= 0).ToList();
                    if (orders != null && orders.Count() > 0)
                    {
                        list = orders;
                    }
                }
            }
            catch (Exception e)
            {
                throw (e);

            }
            return list;
        }
        public void AddError(string title, Exception e)
        {
            try
            {
                var err = new error();
                err.id = Utils.GetUtcTimeDec();
                err.date = DateTime.Now.ToString("yyyyMMddHHmmss");
                err.errmessage = e.Message;
                err.errtitle = title;
                err.errtext = Utils.Exception2String(e);
                using (var db = new CoinTradeDBEntities())
                {
                    db.error.Add(err);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw (ex);

            }
        }
        public void AddReport(report report)
        {
            try
            {
                using (var db = new CoinTradeDBEntities())
                {
                    db.report.Add(report);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw (e);

            }
        }

    }
}
