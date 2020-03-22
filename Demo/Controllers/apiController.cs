using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class apiController : Controller
    {
        #region /getpinyin
        public IActionResult getpinyin()
        {
            return Content("test");
            //return Json(Wlniao.Aliyun.ApiMarket.GetString("http://myapi.wlniao.com", "/getpinyin", "25090927", "3509cffb21cc0a9afd09fc0c6ea6f549"
            //      , new System.Collections.Generic.KeyValuePair<string, string>("str", "重庆")));
        }
        #endregion
    }
}
