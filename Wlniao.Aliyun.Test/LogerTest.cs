using Aliyun.Api.LogService;
using Aliyun.Api.LogService.Domain.Log;

namespace Wlniao.Loger.Test
{
    [TestClass]
    public class LogerTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            Wlniao.Log.Loger.SetLogger(new Wlniao.Aliyun.AliyunLoger(Log.LogLevel.Debug));
            Wlniao.Log.Loger.Topic("test", DateTools.Format());
            System.Threading.Thread.Sleep(1000);
            Wlniao.Log.Loger.Topic("test", DateTools.Format());
            System.Threading.Thread.Sleep(1000);
            Wlniao.Log.Loger.Debug(DateTools.Format());
            System.Threading.Thread.Sleep(1000);
            Wlniao.Log.Loger.Info(DateTools.Format());
            System.Threading.Thread.Sleep(2000);
            Wlniao.Log.Loger.Warn(DateTools.Format());
            System.Threading.Thread.Sleep(1000);
            Wlniao.Log.Loger.Error(DateTools.Format());
            System.Threading.Thread.Sleep(1000);
            Wlniao.Log.Loger.Fatal(DateTools.Format());
            System.Threading.Thread.Sleep(1000);

            Assert.IsTrue(true);
        }
    }
}